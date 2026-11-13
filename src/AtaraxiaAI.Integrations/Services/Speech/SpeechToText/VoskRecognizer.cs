using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Integrations.Downloads;
using AtaraxiaAI.Integrations.DTOs;
using NAudio.Wave;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Vosk;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Integrations.Services;

/// <param name="Name">The model's folder name, which is also the root folder inside the zip.</param>
/// <param name="ExtractedBytes">Roughly the disk space the extracted model needs.</param>
internal sealed record VoskModelDownload(string Name, string Url, long Bytes, string Sha256, long ExtractedBytes);

/// <summary>
/// Offline US English speech recognition with Vosk. The model downloads the first time Vosk is selected, and an
/// interrupted download resumes. Audio streams into the recognizer, which detects the end of each phrase itself.
/// </summary>
/// <remarks>
/// Uses vosk-model-en-us-0.22-lgraph (Apache 2.0) from https://alphacephei.com/vosk/models: a 131 MB download with a
/// word error rate of 7.82 on LibriSpeech test-clean, against 9.85 for the 40 MB small model. The site publishes no
/// checksums, so the size and SHA-256 below were recorded from the official download.
/// </remarks>
internal sealed class VoskRecognizer : IRecognizer
{
    internal const int SAMPLE_RATE = 16000;

    // How long without audio before the phrase in progress is ended with silence.
    private static readonly TimeSpan SILENCE_FLUSH_DELAY = TimeSpan.FromMilliseconds(750);
    private const string MODEL_NAME = "vosk-model-en-us-0.22-lgraph";
    private const string MODEL_URL = "https://alphacephei.com/vosk/models/vosk-model-en-us-0.22-lgraph.zip";
    private const long MODEL_BYTES = 130557655;
    private const string MODEL_SHA256 = "d9838b4aaa82a75c4a17f5aca300eaca129aaab2a7cbf951bafbb500eb9c4334";
    private const long MODEL_EXTRACTED_BYTES = 214028616;

    // Vosk does not report a model it cannot load, so an installation is recognized by these files.
    internal static readonly string[] REQUIRED_FILES = ["am/final.mdl", "conf/model.conf", "graph/Gr.fst", "graph/HCLr.fst"];

    private readonly string _directory;
    private readonly IHttpClientFactory _http;
    private readonly ILogger _logger;
    private readonly VoskModelDownload _download;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _audioLock = new();
    private Model? _model;
    private IWaveIn? _capture;
    private VoskAudioConverter? _converter;
    private ChannelWriter<byte[]>? _audio;
    private Task? _recognition;
    private CancellationTokenSource? _recognitionStop;
    private SoundCaptureSources _captureSource = SoundCaptureSources.SoundCard;
    private bool _disposed;

    internal VoskRecognizer(string directory, IHttpClientFactory http, ILogger logger, VoskModelDownload? download = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _download = download ?? new(MODEL_NAME, MODEL_URL, MODEL_BYTES, MODEL_SHA256, MODEL_EXTRACTED_BYTES);
    }

    internal string ModelPath => Path.Combine(_directory, _download.Name);

    internal bool IsInstalled => REQUIRED_FILES.All(file => File.Exists(Path.Combine(ModelPath, file)));

    internal static bool Supports(CultureInfo culture)
    {
        return string.Equals(culture.Name, "en-US", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsAvailable()
    {
        return _model is not null;
    }

    /// <summary>Downloads and installs the model when it is missing, then loads it.</summary>
    public async Task<bool> PrepareAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        await _gate.WaitAsync(stop.Token).ConfigureAwait(false);

        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_model is not null)
            {
                return true;
            }

            if (!await InstallCoreAsync(progress, stop.Token).ConfigureAwait(false))
            {
                return false;
            }

            _logger.Information("Loading the Vosk speech recognition model.");
            _model = await Task.Run(() =>
            {
                Vosk.Vosk.SetLogLevel(-1);
                return new Model(ModelPath);
            }, stop.Token).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Downloads, verifies, and extracts the model without loading it.</summary>
    internal async Task<bool> InstallAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        await _gate.WaitAsync(stop.Token).ConfigureAwait(false);

        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return await InstallCoreAsync(progress, stop.Token).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void UpdateCaptureSource(SoundCaptureSources source)
    {
        _captureSource = source;
    }

    /// <summary>
    /// Listens to the microphone, or to what the computer is playing when the capture source is the sound card.
    /// </summary>
    /// <remarks>
    /// The capture thread only converts and queues audio; a separate task feeds it to Vosk. Decoding on the capture
    /// thread delays it enough at the end of each phrase that the device drops audio.
    /// </remarks>
    public void Listen(Action<string> speechRecognizedAction)
    {
        ArgumentNullException.ThrowIfNull(speechRecognizedAction);
        Stop();

        lock (_audioLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_model is null)
            {
                throw new InvalidOperationException("The Vosk model has not been prepared.");
            }

            IWaveIn capture = _captureSource == SoundCaptureSources.Microphone
                ? new WaveInEvent { WaveFormat = new WaveFormat(SAMPLE_RATE, 16, 1), BufferMilliseconds = 100 }
                : new WasapiLoopbackCapture();

            // A few minutes of audio; if recognition falls that far behind, the oldest audio is dropped.
            Channel<byte[]> audio = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(2000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true
            });

            Vosk.VoskRecognizer recognizer = new(_model, SAMPLE_RATE);
            try
            {
                _converter = new VoskAudioConverter(capture.WaveFormat);
                _audio = audio.Writer;
                _capture = capture;
                capture.DataAvailable += OnAudioAvailable;
                capture.StartRecording();
            }
            catch
            {
                capture.DataAvailable -= OnAudioAvailable;
                capture.Dispose();
                recognizer.Dispose();
                _converter = null;
                _audio = null;
                _capture = null;
                throw;
            }

            CancellationTokenSource stopping = new();
            _recognitionStop = stopping;
            _recognition = Task.Run(() => RecognizeAsync(audio.Reader, recognizer, speechRecognizedAction, stopping.Token));
        }
    }

    public void Stop()
    {
        IWaveIn? capture;
        ChannelWriter<byte[]>? audio;
        Task? recognition;
        CancellationTokenSource? stopping;

        // Detach under the lock, so audio arriving meanwhile is ignored; stop the device outside it, so its capture
        // thread can finish delivering audio without waiting on this one.
        lock (_audioLock)
        {
            capture = _capture;
            audio = _audio;
            recognition = _recognition;
            stopping = _recognitionStop;
            _capture = null;
            _audio = null;
            _recognition = null;
            _recognitionStop = null;
            _converter = null;
            capture?.DataAvailable -= OnAudioAvailable;
        }

        capture?.StopRecording();
        capture?.Dispose();

        // Cancel rather than let recognition finish the queued audio, so nothing heard before stopping is reported
        // afterward, even if recognition had fallen behind. Recognition then releases its recognizer.
        stopping?.Cancel();
        audio?.TryComplete();
        if (recognition is null)
        {
            stopping?.Dispose();
        }
        else if (recognition.Wait(TimeSpan.FromSeconds(5)))
        {
            stopping?.Dispose();
        }
        else
        {
            _logger.Warning("Vosk recognition did not stop within five seconds.");
        }
    }

    /// <summary>
    /// Transcribes 16 kHz, mono, 16-bit PCM audio, returning each phrase heard. The model must be prepared.
    /// </summary>
    internal IReadOnlyList<string> Transcribe(Stream pcm)
    {
        ArgumentNullException.ThrowIfNull(pcm);
        Model model = _model ?? throw new InvalidOperationException("The Vosk model has not been prepared.");
        List<string> phrases = [];

        using Vosk.VoskRecognizer recognizer = new(model, SAMPLE_RATE);
        byte[] buffer = new byte[4096];
        int read;
        while ((read = pcm.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (recognizer.AcceptWaveform(buffer, read) && ToPhrase(recognizer.Result()) is { } phrase)
            {
                phrases.Add(phrase);
            }
        }

        if (ToPhrase(recognizer.FinalResult()) is { } last)
        {
            phrases.Add(last);
        }

        return phrases;
    }

    /// <summary>
    /// The text of a Vosk result, or <see langword="null"/> for silence and single words, which are usually noise
    /// misheard as words such as "huh" or "the".
    /// </summary>
    internal static string? ToPhrase(string resultJson)
    {
        string? text = JsonSerializer.Deserialize<VoskTranscript>(resultJson)?.Text?.Trim();
        return text is not null && text.Contains(' ') ? text : null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _shutdown.Cancel();
        Stop();
        _gate.Wait();
        try
        {
            _disposed = true;
            _model?.Dispose();
            _model = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void OnAudioAvailable(object? sender, WaveInEventArgs e)
    {
        lock (_audioLock)
        {
            if (ReferenceEquals(sender, _capture) && _converter is not null && _audio is not null)
            {
                byte[] pcm = _converter.Convert(e.Buffer, e.BytesRecorded);
                if (pcm.Length > 0)
                {
                    _audio.TryWrite(pcm);
                }
            }
        }
    }

    /// <summary>
    /// Feeds queued audio to Vosk until listening stops, reporting each phrase heard. Audio still queued when
    /// <paramref name="stopping"/> is canceled is discarded.
    /// </summary>
    private async Task RecognizeAsync(ChannelReader<byte[]> audio, Vosk.VoskRecognizer recognizer, Action<string> speechRecognizedAction, CancellationToken stopping)
    {
        byte[] silence = new byte[SAMPLE_RATE * 2];
        bool ended = true;

        try
        {
            while (!stopping.IsCancellationRequested)
            {
                byte[]? pcm;
                using (CancellationTokenSource quiet = CancellationTokenSource.CreateLinkedTokenSource(stopping))
                {
                    quiet.CancelAfter(SILENCE_FLUSH_DELAY);
                    try
                    {
                        if (!await audio.WaitToReadAsync(quiet.Token).ConfigureAwait(false))
                        {
                            return;
                        }

                        if (!audio.TryRead(out pcm))
                        {
                            continue;
                        }

                        ended = false;
                    }
                    catch (OperationCanceledException) when (stopping.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (OperationCanceledException) when (quiet.IsCancellationRequested)
                    {
                        // A sound card sends nothing while the computer is silent, so Vosk would never hear the pause
                        // that ends a phrase. A second of silence ends it.
                        if (ended)
                        {
                            continue;
                        }

                        pcm = silence;
                        ended = true;
                    }
                }

                if (recognizer.AcceptWaveform(pcm, pcm.Length)
                    && ToPhrase(recognizer.Result()) is { } phrase
                    && !stopping.IsCancellationRequested)
                {
                    speechRecognizedAction(phrase);
                }
            }
        }
        catch (Exception error)
        {
            _logger.Error(error, "Vosk recognition stopped unexpectedly.");
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    private async Task<bool> InstallCoreAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        if (IsInstalled)
        {
            return true;
        }

        string partial = ModelPath + ".zip.download";
        string staging = ModelPath + ".extracting";

        try
        {
            Directory.CreateDirectory(_directory);
            long downloaded = Math.Min(FileDownload.ExistingLength(partial), _download.Bytes);

            if (FileDownload.MissingSpace(_directory, _download.Bytes - downloaded + _download.ExtractedBytes, "Vosk") is { } problem)
            {
                _logger.Warning(problem);
                return false;
            }

            _logger.Information("Downloading the Vosk speech recognition model ({Size}).", FileDownload.FormatSize(_download.Bytes));
            DownloadProgress reporter = new(progress is null ? null : (bytes, total) => progress.Report(bytes * 100d / total), _download.Bytes, downloaded);
            using HttpClient client = _http.CreateClient();
            await FileDownload.DownloadAsync(client, new Uri(_download.Url), partial, _download.Bytes, reporter, cancellationToken).ConfigureAwait(false);
            reporter.Report(force: true);

            if (FileDownload.ExistingLength(partial) != _download.Bytes
                || !string.Equals(await FileDownload.HashAsync(partial, cancellationToken).ConfigureAwait(false), _download.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(partial);
                _logger.Warning("The Vosk download failed its integrity check and was discarded. Select Vosk again to retry.");
                return false;
            }

            // Extract beside the final location and move the model into place only once complete, so an interrupted
            // extraction is never mistaken for an installed model.
            DeleteDirectory(staging);
            await ZipFile.ExtractToDirectoryAsync(partial, staging, cancellationToken).ConfigureAwait(false);
            string extracted = Path.Combine(staging, _download.Name);
            if (!REQUIRED_FILES.All(file => File.Exists(Path.Combine(extracted, file))))
            {
                _logger.Warning("The Vosk download does not contain the expected model files.");
                return false;
            }

            DeleteDirectory(ModelPath);
            Directory.Move(extracted, ModelPath);
            File.Delete(partial);
            _logger.Information("Vosk speech recognition model installed.");
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error) when (error is HttpRequestException or IOException or InvalidDataException or UnauthorizedAccessException or TimeoutException)
        {
            _logger.Warning(error, "Could not install Vosk. Select Vosk again to resume the download.");
            return false;
        }
        finally
        {
            DeleteDirectory(staging);
        }
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}

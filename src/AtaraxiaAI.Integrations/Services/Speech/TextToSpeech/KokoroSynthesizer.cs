using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Integrations.Downloads;
using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using Microsoft.ML.OnnxRuntime;
using NAudio.Utils;
using NAudio.Wave;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Integrations.Services;

internal sealed record KokoroDownload(string Url, long Bytes, string Sha256);

/// <summary>
/// Local English speech synthesis with Kokoro, run in this process through KokoroSharp and ONNX Runtime. The
/// model is downloaded once, the first time Kokoro is selected, and an interrupted download resumes.
/// </summary>
/// <remarks>Adapted from HomiliaMessenger's KokoroSpeech.</remarks>
internal sealed class KokoroSynthesizer : ISynthesizer, IDisposable
{
    internal const long MODEL_BYTES = 325508342;
    internal const string MODEL_SHA256 = "0cfd5e79aab70a3d8c1a57dc639835110ddb32c9f5ff4fdd1f4db202ea43bb05";
    private const string MODEL_URL = "https://github.com/Lyrcaxis/KokoroSharpBinaries/releases/download/v2.0.0/kokoro.onnx";
    private const int SAMPLE_RATE = 24000;

    private readonly string _directory;
    private readonly IHttpClientFactory _http;
    private readonly ILogger _logger;
    private readonly KokoroDownload _download;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private KokoroModel? _model;
    // Voices loaded for this model, so each utterance does not read and parse its voice file again.
    private readonly Dictionary<string, KokoroVoice> _voices = new(StringComparer.Ordinal);
    private SessionOptions? _options;
    private bool _disposed;

    internal KokoroSynthesizer(string directory, IHttpClientFactory http, ILogger logger, KokoroDownload? download = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _download = download ?? new(MODEL_URL, MODEL_BYTES, MODEL_SHA256);
    }

    internal string ModelPath => Path.Combine(_directory, "kokoro.onnx");

    /// <summary>The English voices bundled with KokoroSharp, which all share the one downloaded model.</summary>
    public IReadOnlyList<SpeechVoice> Voices { get; } = CreateVoices();

    internal static bool Supports(CultureInfo culture)
    {
        return string.Equals(culture.Name, "en-US", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsAvailable()
    {
        return IsDownloaded();
    }

    /// <summary>
    /// Downloads and verifies the model when it is missing. A failed, stalled, or canceled download keeps what
    /// arrived, and the next attempt resumes it.
    /// </summary>
    public async Task<bool> PrepareAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        await _gate.WaitAsync(stop.Token).ConfigureAwait(false);
        string partial = ModelPath + ".download";

        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsDownloaded())
            {
                return true;
            }

            Directory.CreateDirectory(_directory);
            long downloaded = Math.Min(FileDownload.ExistingLength(partial), _download.Bytes);

            if (FileDownload.MissingSpace(_directory, _download.Bytes - downloaded, "Kokoro") is { } problem)
            {
                _logger.Warning(problem);
                return false;
            }

            _logger.Information("Downloading the Kokoro speech model ({Size}).", FileDownload.FormatSize(_download.Bytes));
            DownloadProgress reporter = new(progress is null ? null : (bytes, total) => progress.Report(bytes * 100d / total), _download.Bytes, downloaded);
            using HttpClient client = _http.CreateClient();
            await FileDownload.DownloadAsync(client, new Uri(_download.Url), partial, _download.Bytes, reporter, stop.Token).ConfigureAwait(false);
            reporter.Report(force: true);

            if (FileDownload.ExistingLength(partial) != _download.Bytes
                || !string.Equals(await FileDownload.HashAsync(partial, stop.Token).ConfigureAwait(false), _download.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(partial);
                _logger.Warning("The Kokoro download failed its integrity check and was discarded. Select Kokoro again to retry.");
                return false;
            }

            File.Move(partial, ModelPath, overwrite: true);
            _logger.Information("Kokoro speech model downloaded.");
            return true;
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error) when (error is HttpRequestException or IOException or InvalidDataException or UnauthorizedAccessException or TimeoutException)
        {
            _logger.Warning(error, "Could not download Kokoro. Select Kokoro again to resume the download.");
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<byte[]> SynthesizeAsync(string message, string? voiceId, CancellationToken cancellationToken)
    {
        using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        await _gate.WaitAsync(stop.Token).ConfigureAwait(false);

        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!IsDownloaded())
            {
                throw new InvalidOperationException("The Kokoro speech model has not been downloaded.");
            }

            string text = PrepareText(message ?? string.Empty);
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            // Native inference and phonemization must never run on the UI thread. The core model API is used so
            // failures surface on this task rather than on a library-owned background thread.
            return await Task.Run(() =>
            {
                if (_model is null)
                {
                    _options = new SessionOptions { InterOpNumThreads = 1, IntraOpNumThreads = 2 };
                    _model = new KokoroModel(ModelPath, _options);
                }

                // Only listed voices are loaded, so a voice ID can never name an arbitrary file.
                string id = Voices.FirstOrDefault(voice => voice.Id == voiceId)?.Id ?? Voices[0].Id;
                if (!_voices.TryGetValue(id, out KokoroVoice? voice))
                {
                    voice = KokoroVoice.FromPath(Path.Combine(AppContext.BaseDirectory, "SpeechVoices", id + ".npy"));
                    _voices[id] = voice;
                }

                using MemoryStream pcm = new();
                foreach (int[] segment in Segments(Tokenizer.Tokenize(text, id[0] == 'b' ? "en-gb" : "en-us")))
                {
                    stop.Token.ThrowIfCancellationRequested();
                    float[] samples = _model.Infer(segment, voice.Features);
                    pcm.Write(KokoroPlayback.GetBytes(samples));
                }

                return ToWav(pcm.ToArray());
            }, stop.Token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            Unload();
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Splits tokens at punctuation, so long messages are not cut mid-word. Any segment still over the model's
    /// limit is split further.
    /// </summary>
    internal static IEnumerable<int[]> Segments(int[] tokens)
    {
        foreach (int[] segment in SegmentationSystem.SplitToSegments(tokens, new DefaultSegmentationConfig()))
        {
            foreach (int[] part in segment.Chunk(KokoroModel.maxTokens))
            {
                yield return part;
            }
        }
    }

    /// <summary>Prepares a speech-only copy of a message for Kokoro's English phonemizer.</summary>
    /// <remarks>Adapted from HomiliaMessenger's SpeechText.</remarks>
    internal static string PrepareText(string text)
    {
        StringBuilder cleaned = new(text.Length);

        // EnumerateRunes consumes a valid surrogate pair together and substitutes U+FFFD for malformed UTF-16.
        // The phonemizer's tokenization can split even valid supplementary characters into isolated surrogates,
        // which then fail Unicode normalization, so keep them out.
        foreach (Rune rune in text.EnumerateRunes())
        {
            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            bool noncharacter = rune.Value is >= 0xFDD0 and <= 0xFDEF || (rune.Value & 0xFFFE) == 0xFFFE;
            if (!rune.IsBmp || rune == Rune.ReplacementChar || noncharacter
                || category == UnicodeCategory.Format
                || (category == UnicodeCategory.Control && !Rune.IsWhiteSpace(rune)))
            {
                // A separator avoids joining the words on either side of an omitted symbol.
                cleaned.Append(' ');
            }
            else
            {
                cleaned.Append(rune.ToString());
            }
        }

        return cleaned.ToString();
    }

    /// <summary>Wraps KokoroSharp's 24 kHz, mono, 16-bit PCM in a WAV file.</summary>
    internal static byte[] ToWav(byte[] pcm)
    {
        using MemoryStream wav = new();
        using (WaveFileWriter writer = new(new IgnoreDisposeStream(wav), new WaveFormat(SAMPLE_RATE, 16, 1)))
        {
            writer.Write(pcm, 0, pcm.Length);
        }

        return wav.ToArray();
    }

    /// <remarks>Adapted from HomiliaMessenger's KokoroSpeech voice list.</remarks>
    private static IReadOnlyList<SpeechVoice> CreateVoices()
    {
        List<SpeechVoice> voices = [];
        // Heart is first, the default voice.
        Add("af", VoiceGender.Female, "American", "heart", "alloy", "aoede", "bella", "jessica", "kore", "nicole", "nova", "river", "sarah", "sky");
        Add("am", VoiceGender.Male, "American", "adam", "echo", "eric", "fenrir", "liam", "michael", "onyx", "puck", "santa");
        Add("bf", VoiceGender.Female, "British", "alice", "emma", "isabella", "lily");
        Add("bm", VoiceGender.Male, "British", "daniel", "fable", "george", "lewis");
        return voices;

        void Add(string prefix, VoiceGender gender, string accent, params string[] names)
        {
            foreach (string name in names)
            {
                voices.Add(new SpeechVoice(prefix + "_" + name, $"{char.ToUpperInvariant(name[0])}{name[1..]} ({accent})", gender));
            }
        }
    }

    private bool IsDownloaded()
    {
        FileInfo file = new(ModelPath);
        return file.Exists && file.Length == _download.Bytes;
    }

    private void Unload()
    {
        _voices.Clear();
        _model?.Dispose();
        _model = null;
        _options?.Dispose();
        _options = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _shutdown.Cancel();
        _gate.Wait();
        try
        {
            _disposed = true;
            Unload();
        }
        finally
        {
            _gate.Release();
        }
    }
}

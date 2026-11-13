using AtaraxiaAI.Business.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Componants;

public class SpeechEngine
{
    public const SpeechSynthesizers DEFAULT_SYNTHESIZER = SpeechSynthesizers.SystemDotSpeech;
    private const SpeechSynthesizers FALLBACK_SYNTHESIZER = SpeechSynthesizers.SystemDotSpeech;
    private const string VOICE_SAMPLE = "Hello! This is how I sound.";

    /// <summary>Speech sources the user can choose, in display order.</summary>
    public static IReadOnlyList<SpeechSynthesizers> SelectableSynthesizers { get; } =
    [
        SpeechSynthesizers.SystemDotSpeech,
        SpeechSynthesizers.Kokoro
    ];

    public bool IsSpeechRecognitionRunning { get; set; }

    /// <summary>The speech source in use, or <see langword="null"/> when none is available.</summary>
    public SpeechSynthesizers? ActiveSynthesizer => _active?.Kind;

    /// <summary>The voices the speech source in use offers, the default first; empty when it offers no choice.</summary>
    public IReadOnlyList<SpeechVoice> ActiveVoices => _active?.Synthesizer.Voices ?? [];

    /// <summary>
    /// The chosen voice. A speech source that does not offer this voice speaks with its default voice.
    /// </summary>
    public string? VoiceId { get; set; }

    private readonly CultureInfo _culture;
    private readonly IIntegrationFactory _integrations;
    private readonly IAudioPlayer _player;
    private readonly IntegrationDependencies _providerContext;
    private readonly CancellationTokenSource _playbackCancellation = new();
    private readonly SemaphoreSlim _selectionGate = new(1, 1);
    private readonly object _activeLock = new();
    private int _commandQueued;
    private ActiveSpeech? _active;
    private readonly OrchestrationEngine _commandLoop;
    private readonly IRecognizer _recognizer;

    internal SpeechEngine(IIntegrationFactory integrations, IAudioPlayer player, IntegrationDependencies providerContext, CultureInfo? culture = null)
    {
        _integrations = integrations;
        _player = player;
        _providerContext = providerContext;
        _culture = culture ?? new CultureInfo("en-US");
        _commandLoop = new OrchestrationEngine(this, integrations, providerContext.Logger);
        _recognizer = _integrations.CreateRecognizer(_culture);
    }

    /// <summary>
    /// Switches speech to <paramref name="requested"/>, first preparing it, which may download a model. If it cannot
    /// be prepared, the current source stays in use, or the system voice is used when there is none.
    /// </summary>
    /// <param name="progress">Receives download progress from 0 to 100 when the source must download something.</param>
    /// <returns>Whether <paramref name="requested"/> is now in use.</returns>
    /// <exception cref="OperationCanceledException">The selection was canceled; the current source stays in use.</exception>
    public async Task<bool> SelectSynthesizerAsync(SpeechSynthesizers requested, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        await _selectionGate.WaitAsync(cancellationToken);
        try
        {
            ActiveSpeech? current = _active;
            if (current?.Kind == requested)
            {
                return true;
            }

            ActiveSpeech? prepared = await PrepareAsync(requested, progress, cancellationToken);
            if (prepared is not null)
            {
                SetActive(prepared);
                _providerContext.Logger.Information("Speech source set to {Provider}.", requested);
                return true;
            }

            if (current is not null)
            {
                _providerContext.Logger.Warning("{Provider} is unavailable; still using {Current}.", requested, current.Kind);
                return false;
            }

            if (requested != FALLBACK_SYNTHESIZER && await PrepareAsync(FALLBACK_SYNTHESIZER, null, cancellationToken) is { } fallback)
            {
                SetActive(fallback);
                _providerContext.Logger.Warning("{Provider} is unavailable; using {Fallback}.", requested, FALLBACK_SYNTHESIZER);
                return false;
            }

            _providerContext.Logger.Warning("No speech synthesizer is currently available.");
            return false;
        }
        finally
        {
            _selectionGate.Release();
        }
    }

    internal async Task SpeakAsync(string message, CancellationToken cancellationToken)
    {
        ActiveSpeech? active = _active;
        if (active is null)
        {
            return;
        }

        try
        {
            await SynthesizeAndPlayAsync(active, message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            _providerContext.Logger.Error(error, "Speech synthesis or playback failed with {Provider}.", active.Kind);
            if (active.Kind == FALLBACK_SYNTHESIZER || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                if (await PrepareAsync(FALLBACK_SYNTHESIZER, null, cancellationToken) is { } fallback)
                {
                    lock (_activeLock)
                    {
                        // Keep a source the user selected while this utterance was failing.
                        if (ReferenceEquals(_active, active))
                        {
                            _active = fallback;
                        }
                    }

                    _providerContext.Logger.Warning("Falling back to {Fallback} speech.", FALLBACK_SYNTHESIZER);
                    await SynthesizeAndPlayAsync(fallback, message, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception fallbackError)
            {
                _providerContext.Logger.Error(fallbackError, "Fallback speech synthesis or playback failed.");
            }
        }
    }

    private async Task SynthesizeAndPlayAsync(ActiveSpeech speech, string message, CancellationToken cancellationToken)
    {
        _providerContext.Logger.Information("Generating speech with {Provider}.", speech.Kind);
        byte[] wav = await speech.Synthesizer.SynthesizeAsync(message, VoiceId, cancellationToken);
        if (wav == null || wav.Length == 0)
        {
            return;
        }

        _providerContext.Logger.Information($"*Speaking* \"{message}\"");
        await _player.PlayAsync(wav, cancellationToken);
    }

    /// <summary>
    /// Speaks a short sample with one of <see cref="ActiveVoices"/>, so the user can hear it before choosing it.
    /// Failures are logged rather than thrown.
    /// </summary>
    public async Task PlayVoiceSampleAsync(string voiceId, CancellationToken cancellationToken = default)
    {
        ActiveSpeech? active = _active;
        if (active is null || !active.Synthesizer.Voices.Any(voice => voice.Id == voiceId))
        {
            return;
        }

        try
        {
            byte[] wav = await active.Synthesizer.SynthesizeAsync(VOICE_SAMPLE, voiceId, cancellationToken);
            if (wav is { Length: > 0 })
            {
                await _player.PlayAsync(wav, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            _providerContext.Logger.Error(error, "Could not play a sample of the {Voice} voice.", voiceId);
        }
    }

    private async Task<ActiveSpeech?> PrepareAsync(SpeechSynthesizers kind, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        try
        {
            ISynthesizer candidate = _integrations.CreateSynthesizer(kind, _culture);
            if (await candidate.PrepareAsync(progress, cancellationToken) && candidate.IsAvailable())
            {
                return new ActiveSpeech(kind, candidate);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            _providerContext.Logger.Warning(error, "Speech provider {Provider} unavailable.", kind);
        }

        return null;
    }

    private void SetActive(ActiveSpeech speech)
    {
        lock (_activeLock)
        {
            _active = speech;
        }
    }

    private void OnSpeechRecognized(string message)
    {
        // System.Speech invokes this on its recognition callback. Return before stopping
        // recognition, performing HTTP requests, or loading the synthesis model.
        if (Interlocked.CompareExchange(ref _commandQueued, 1, 0) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                if (IsSpeechRecognitionRunning)
                {
                    await _commandLoop.HeardAsync(message, _playbackCancellation.Token);
                }
            }
            catch (OperationCanceledException) when (_playbackCancellation.IsCancellationRequested)
            {
            }
            catch (Exception error)
            {
                _providerContext.Logger.Error(error, "Failed to process spoken command.");
            }
            finally
            {
                Interlocked.Exchange(ref _commandQueued, 0);
            }
        });
    }

    public void CancelPlayback()
    {
        _playbackCancellation.Cancel();
    }

    public void ActivateSpeechRecognition()
    {
        _providerContext.Logger.Information("Beginning speech recognition.");

        DeactivateSpeechRecognition();
        _recognizer.Listen(OnSpeechRecognized);
        IsSpeechRecognitionRunning = true;
    }

    public void DeactivateSpeechRecognition()
    {
        if (IsSpeechRecognitionRunning)
        {
            _recognizer.Dispose();
            _providerContext.Logger.Information("Ended speech recognition.");
            IsSpeechRecognitionRunning = false;
        }
    }

    public void UpdateCaptureSource(SoundCaptureSources captureSource)
    {
        bool wasRunningWhenChangeMade = IsSpeechRecognitionRunning;

        if (wasRunningWhenChangeMade)
        {
            DeactivateSpeechRecognition();
        }

        _recognizer.UpdateCaptureSource(captureSource);

        if (wasRunningWhenChangeMade)
        {
            ActivateSpeechRecognition();
        }
    }

    private sealed record ActiveSpeech(SpeechSynthesizers Kind, ISynthesizer Synthesizer);
}

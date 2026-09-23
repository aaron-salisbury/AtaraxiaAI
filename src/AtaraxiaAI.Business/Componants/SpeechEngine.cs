using AtaraxiaAI.Business.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Componants
{
    public class SpeechEngine
    {
        public bool IsSpeechRecognitionRunning { get; set; }

        private readonly CultureInfo _culture;
        private readonly IIntegrationFactory _integrations;
        private readonly IAudioPlayer _player;
        private readonly IntegrationDependencies _providerContext;
        private readonly CancellationTokenSource _playbackCancellation = new();
        private readonly HashSet<SpeechSynthesizers> _failedSynthesizers = new();
        private SpeechSynthesizers? _selectedSynthesizer;
        private OrchestrationEngine _commandLoop { get; set; }
        private IRecognizer _recognizer;
        private ISynthesizer _synthesizer;

        internal SpeechEngine(IIntegrationFactory integrations, IAudioPlayer player, IntegrationDependencies providerContext, CultureInfo culture = null)
        {
            _integrations = integrations;
            _player = player;
            _providerContext = providerContext;
            _culture = culture ?? new CultureInfo("en-US");
            _commandLoop = new OrchestrationEngine(this, integrations, providerContext.Logger);
            _recognizer = _integrations.CreateRecognizer(_culture);

            SetSynthesizer();
        }

        internal void Speak(string message) => SpeakAsync(message, _playbackCancellation.Token).GetAwaiter().GetResult();

        internal async Task SpeakAsync(string message, CancellationToken cancellationToken)
        {
            if (_synthesizer == null) return;
            bool wasListening = IsSpeechRecognitionRunning;
            if (wasListening) _recognizer.Pause();
            try
            {
                byte[] wav = await _synthesizer.SynthesizeAsync(message, cancellationToken);
                if (wav == null || wav.Length == 0) return;
                _providerContext.Logger.Information($"*Speaking* \"{message}\"");
                await _player.PlayAsync(wav, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception error)
            {
                _providerContext.Logger.Error(error, "Speech synthesis or playback failed.");
                if (_selectedSynthesizer is { } failed) _failedSynthesizers.Add(failed);
                SetSynthesizer();
            }
            finally
            {
                if (wasListening && IsSpeechRecognitionRunning && !cancellationToken.IsCancellationRequested)
                    _recognizer.Unpause();
            }
        }

        internal void SetSynthesizer(SpeechSynthesizers? requested = null)
        {
            SpeechSynthesizers[] choices = requested is { } selection
                ? new[] { selection, SpeechSynthesizers.Kokoro, SpeechSynthesizers.SystemDotSpeech }
                : new[] { SpeechSynthesizers.Kokoro, SpeechSynthesizers.SystemDotSpeech };
            if (requested is { } retry) _failedSynthesizers.Remove(retry);
            _synthesizer = null;
            _selectedSynthesizer = null;
            foreach (var choice in choices)
            {
                if (_failedSynthesizers.Contains(choice)) continue;
                try
                {
                    var candidate = _integrations.CreateSynthesizer(choice, _culture);
                    if (candidate.IsAvailable())
                    {
                        _synthesizer = candidate;
                        _selectedSynthesizer = choice;
                        return;
                    }
                }
                catch (Exception error) { _providerContext.Logger.Warning(error, "Speech provider {Provider} unavailable.", choice); }
            }
            _providerContext.Logger.Warning("No speech synthesizer is currently available.");
        }

        public void CancelPlayback() => _playbackCancellation.Cancel();

        public void ActivateSpeechRecognition()
        {
            _providerContext.Logger.Information("Beginning speech recognition.");

            DeactivateSpeechRecognition();
            _recognizer.Listen(_commandLoop.Heard);
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

    }
}

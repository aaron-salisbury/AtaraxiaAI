using AtaraxiaAI.Business.Services;
using System;
using System.Threading.Tasks;
using System.Globalization;

using System.Threading;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Componants
{
    public class SpeechEngine
    {
        public bool IsSpeechRecognitionRunning { get; set; }

        private readonly CultureInfo _culture;
        private readonly IIntegrationFactory _integrations;
        private readonly IAudioPlayer _player;
        private readonly CancellationTokenSource _playbackCancellation = new();
        private OrchestrationEngine _commandLoop { get; set; }
        private IRecognizer _recognizer;
        private ISynthesizer _synthesizer;

        internal SpeechEngine(IIntegrationFactory integrations, IAudioPlayer player, CultureInfo culture = null)
        {
            _integrations = integrations;
            _player = player;
            _culture = culture ?? new CultureInfo("en-US");
            _commandLoop = new OrchestrationEngine(this);
            _recognizer = _integrations.CreateRecognizer(_culture);

            SetSynthesizer();
        }

        internal void Speak(string message) => SpeakAsync(message, _playbackCancellation.Token).GetAwaiter().GetResult();

        internal async Task SpeakAsync(string message, CancellationToken cancellationToken)
        {
            if (_synthesizer == null) return;
            _recognizer.Pause();
            try
            {
                byte[] wav = await _synthesizer.SynthesizeAsync(message, cancellationToken);
                if (wav == null || wav.Length == 0) return;
                AI.Logger.Information($"*Speaking* \"{message}\"");
                await _player.PlayAsync(wav, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception error)
            {
                AI.Logger.Error(error, "Speech synthesis or playback failed.");
                SetSynthesizer();
            }
            finally { _recognizer.Unpause(); }
        }

        internal void SetSynthesizer(SpeechSynthesizers? requested = null)
        {
            SpeechSynthesizers[] choices = requested is { } selection
                ? new[] { selection, SpeechSynthesizers.Kokoro, SpeechSynthesizers.SystemDotSpeech }
                : new[] { SpeechSynthesizers.Kokoro, SpeechSynthesizers.SystemDotSpeech };
            _synthesizer = null;
            foreach (var choice in choices)
            {
                try
                {
                    var candidate = _integrations.CreateSynthesizer(choice, _culture);
                    if (candidate.IsAvailable()) { _synthesizer = candidate; return; }
                }
                catch (Exception error) { AI.Logger.Warning(error, "Speech provider {Provider} unavailable.", choice); }
            }
            AI.Logger.Warning("No speech synthesizer is currently available.");
        }

        public void CancelPlayback() => _playbackCancellation.Cancel();

        public void ActivateSpeechRecognition()
        {
            AI.Logger.Information("Beginning speech recognition.");

            DeactivateSpeechRecognition();
            _recognizer.Listen(_commandLoop.Heard);
            IsSpeechRecognitionRunning = true;
        }

        public void DeactivateSpeechRecognition()
        {
            if (IsSpeechRecognitionRunning)
            {
                _recognizer.Dispose();
                AI.Logger.Information("Ended speech recognition.");
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

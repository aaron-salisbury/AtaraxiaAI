using AtaraxiaAI.Business.Services;
using NAudio.Wave;
using System.Globalization;
using System.IO;
using System.Threading;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Componants
{
    public class SpeechEngine
    {
        public bool IsSpeechRecognitionRunning { get; set; }

        private CultureInfo _culture;
        private OrchestrationEngine _commandLoop { get; set; }
        private IRecognizer _recognizer;
        private ISynthesizer _synthesizer;

        internal SpeechEngine(CultureInfo culture = null)
        {
            _culture = culture ?? new CultureInfo("en-US");
            _commandLoop = new OrchestrationEngine(this);
            _recognizer = new SystemDotSpeechRecognizer(_culture);

            SetSynthesizer();
        }

        internal void Speak(string message)
        {
            if (_synthesizer != null) // Could be null if cloud services are maxed and also not running on Windows.
            {
                _recognizer.Pause();
                bool spoke = _synthesizer.SpeakAsync(message).Result;
                _recognizer.Unpause();

                if (!spoke)
                {
                    SetSynthesizer();
                    Speak(message);
                }
            }
        }

        internal void SetSynthesizer(SpeechSynthesizers? synthesizerRequest = null)
        {
            if (synthesizerRequest != null)
            {
                ISynthesizer requestedSynthesizer = null;

                switch (synthesizerRequest.Value)
                {
                    case SpeechSynthesizers.GoogleCloud:
                        requestedSynthesizer = new GoogleCloudSynthesizer(_culture);
                        break;
                    case SpeechSynthesizers.MicrosoftAzure:
                        requestedSynthesizer = new MicrosoftAzureSynthesizer(_culture);
                        break;
                    case SpeechSynthesizers.MicrosoftBing:
                        requestedSynthesizer = new MicrosoftBingSynthesizer(_culture);
                        break;
                    case SpeechSynthesizers.SystemDotSpeech:
                        requestedSynthesizer = new SystemDotSpeechSynthesizer(_culture);
                        break;
                }

                if (requestedSynthesizer != null && requestedSynthesizer.IsAvailable())
                {
                    _synthesizer = requestedSynthesizer;
                    return;
                }
            }

            ISynthesizer synthesizer = new GoogleCloudSynthesizer(_culture);
            if (synthesizer.IsAvailable())
            {
                _synthesizer = synthesizer;
                return;
            }

            synthesizer = new MicrosoftAzureSynthesizer(_culture);
            if (synthesizer.IsAvailable())
            {
                _synthesizer = synthesizer;
                return;
            }

            synthesizer = new MicrosoftBingSynthesizer(_culture);
            if (synthesizer.IsAvailable())
            {
                _synthesizer = synthesizer;
                return;
            }

            synthesizer = new SystemDotSpeechSynthesizer(_culture);
            if (synthesizer.IsAvailable())
            {
                _synthesizer = synthesizer;
                return;
            }

            _synthesizer = null;
            AI.Logger.Warning("No speech synthesizer is currently available.");
        }

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

            if (_recognizer is VoskRecognizer voskRecognizer)
            {
                voskRecognizer.CaptureSource = captureSource;
            }

            if (wasRunningWhenChangeMade)
            {
                ActivateSpeechRecognition();
            }
        }

        internal static void StreamSpeechToSpeaker(byte[] speechWavBuffer, string originalText = null)
        {
            using (var ms = new MemoryStream(speechWavBuffer))
            using (var rdr = new WaveFileReader(ms))
            using (var wavStream = WaveFormatConversionStream.CreatePcmStream(rdr))
            using (var provider = new BlockAlignReductionStream(wavStream))
            using (var waveOut = new WaveOutEvent())
            {
                if (!string.IsNullOrEmpty(originalText))
                {
                    AI.Logger.Information($"*Speaking* \"{originalText}\"");
                }

                waveOut.Init(provider);
                waveOut.Play();
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}

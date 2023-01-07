using AtaraxiaAI.Business.Services;
using NAudio.Wave;
using System.Globalization;
using System.IO;
using System.Threading;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Componants
{
    internal class SpeechEngine
    {
        internal IRecognizer Recognizer { get; set; }
        internal ISynthesizer Synthesizer { get; set; }

        private CultureInfo _culture;

        internal SpeechEngine(CultureInfo culture = null)
        {
            _culture = culture ?? new CultureInfo("en-US");

            Recognizer = new VoskRecognizer();

            SetSynthesizer();
        }

        internal void Speak(string message)
        {
            if (Synthesizer != null) // Could be null if cloud services are maxed and also not running on Windows.
            {
                Recognizer.Pause();
                bool spoke = Synthesizer.SpeakAsync(message).Result;
                Recognizer.Unpause();

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
                    case SpeechSynthesizers.Google:
                        requestedSynthesizer = new GoogleCloudSynthesizer(_culture);
                        break;
                    case SpeechSynthesizers.Microsoft:
                        requestedSynthesizer = new MicrosoftAzureSynthesizer(_culture);
                        break;
                    case SpeechSynthesizers.SystemDotSpeech:
                        requestedSynthesizer = new SystemDotSpeechSynthesizer(_culture);
                        break;
                }

                if (requestedSynthesizer != null && requestedSynthesizer.IsAvailable())
                {
                    Synthesizer = requestedSynthesizer;
                    return;
                }
            }

            ISynthesizer synthesizer = new GoogleCloudSynthesizer(_culture);
            if (synthesizer.IsAvailable())
            {
                Synthesizer = synthesizer;
                return;
            }

            synthesizer = new MicrosoftAzureSynthesizer(_culture);
            if (synthesizer.IsAvailable())
            {
                Synthesizer = synthesizer;
                return;
            }

            synthesizer = new SystemDotSpeechSynthesizer(_culture);
            if (synthesizer.IsAvailable())
            {
                Synthesizer = synthesizer;
                return;
            }

            Synthesizer = null;
            AI.Log.Logger.Warning("No speech synthesizer is currently available.");
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
                    AI.Log.Logger.Information($"*Speaking* \"{originalText}\"");
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

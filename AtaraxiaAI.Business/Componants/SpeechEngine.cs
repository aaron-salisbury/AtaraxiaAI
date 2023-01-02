using AtaraxiaAI.Business.Services;
using NAudio.Wave;
using System.Globalization;
using System.IO;
using System.Threading;

namespace AtaraxiaAI.Business.Componants
{
    public class SpeechEngine
    {
        public IRecognizer Recognizer { get; set; }
        public ISynthesizer Synthesizer { get; set; }

        private CultureInfo _culture;

        public SpeechEngine(CultureInfo culture = null)
        {
            AI.Log.Logger.Information("Initializing speech engine.");

            _culture = culture ?? new CultureInfo("en-US");

            Recognizer = new SystemDotSpeechRecognizer(_culture);

            SetSynthesizer();
        }

        public void Speak(string message)
        {
            if (Synthesizer != null) // Could be null if cloud services are maxed and also not running on Windows.
            {
                if (!Synthesizer.SpeakAsync(message).Result)
                {
                    SetSynthesizer();
                    Speak(message);
                }
            }
        }

        private void SetSynthesizer()
        {
            ISynthesizer googleSynthesizer = new GoogleCloudSynthesizer(_culture);
            if (googleSynthesizer.IsAvailable())
            {
                Synthesizer = googleSynthesizer;
                return;
            }

            ISynthesizer microsoftSynthesizer = new MicrosoftAzureSynthesizer(_culture);
            if (microsoftSynthesizer.IsAvailable())
            {
                Synthesizer = microsoftSynthesizer;
                return;
            }

            ISynthesizer systemDotSpeechSynthesizer = new SystemDotSpeechSynthesizer(_culture);
            if (systemDotSpeechSynthesizer.IsAvailable())
            {
                Synthesizer = systemDotSpeechSynthesizer;
                return;
            }

            Synthesizer = null;
            AI.Log.Logger.Warning("No speech synthesizer is currently available.");
        }

        public static void StreamSpeechToSpeaker(byte[] speechWavBuffer)
        {
            using (var ms = new MemoryStream(speechWavBuffer))
            using (var rdr = new WaveFileReader(ms))
            using (var wavStream = WaveFormatConversionStream.CreatePcmStream(rdr))
            using (var provider = new BlockAlignReductionStream(wavStream))
            using (var waveOut = new WaveOutEvent())
            {
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

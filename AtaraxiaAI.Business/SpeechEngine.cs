using System.Globalization;

namespace AtaraxiaAI.Business.Services
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
            if (Synthesizer != null) // Could be null if cloud services are maxed and not running on Windows.
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
            ISynthesizer microsoftSynthesizer = new MicrosoftAzureSynthesizer(_culture);
            if (microsoftSynthesizer.IsAvailable())
            {
                Synthesizer = microsoftSynthesizer;
                return;
            }

            ISynthesizer googleSynthesizer = new GoogleCloudSynthesizer(_culture);
            if (googleSynthesizer.IsAvailable())
            {
                Synthesizer = googleSynthesizer;
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
    }
}

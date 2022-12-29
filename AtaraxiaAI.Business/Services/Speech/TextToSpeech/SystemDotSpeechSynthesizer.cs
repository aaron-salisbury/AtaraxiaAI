using AtaraxiaAI.Business.Componants;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public class SystemDotSpeechSynthesizer : ISynthesizer
    {
        private CultureInfo _culture;

        public SystemDotSpeechSynthesizer(CultureInfo culture = null)
        {
            _culture = culture ?? new CultureInfo("en-US");
        }

        public bool IsAvailable() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public Task<bool> SpeakAsync(string message)
        {
            bool isSuccessful = false;

            if (IsAvailable())
            {
                using (var synthesizer = new SpeechSynthesizer())
                using (var audioStream = new MemoryStream())
                {
                    synthesizer.SetOutputToWaveStream(audioStream);

                    PromptBuilder promptBuilder = new PromptBuilder();
                    promptBuilder.StartVoice(_culture);
                    promptBuilder.AppendText(message);
                    promptBuilder.EndVoice();

                    synthesizer.Speak(promptBuilder);
                    SpeechEngine.StreamSpeechToSpeaker(audioStream.GetBuffer());
                    isSuccessful = true;
                }
            }

            return Task.FromResult(isSuccessful);
        }
    }
}

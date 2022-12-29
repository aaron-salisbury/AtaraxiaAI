using System.Globalization;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public class SystemDotSpeechSynthesizer : ISynthesizer
    {
        private SpeechSynthesizer _synthesizer;
        private CultureInfo _culture;

        public SystemDotSpeechSynthesizer(CultureInfo culture = null)
        {
            if (IsAvailable())
            {
                _culture = culture ?? new CultureInfo("en-US");
                _synthesizer = new SpeechSynthesizer();
                _synthesizer.SetOutputToDefaultAudioDevice();
            }
        }

        public bool IsAvailable() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public async Task<bool> SpeakAsync(string message)
        {
            bool isSuccessful = false;

            if (IsAvailable())
            {
                PromptBuilder promptBuilder = new PromptBuilder();
                promptBuilder.StartVoice(_culture);
                promptBuilder.AppendText(message);
                promptBuilder.EndVoice();

                await Task.Run(() => _synthesizer.SpeakAsync(promptBuilder));
                isSuccessful = true;
            }

            return isSuccessful;
        }
    }
}

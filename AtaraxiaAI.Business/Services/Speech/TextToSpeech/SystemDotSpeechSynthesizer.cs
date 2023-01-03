using AtaraxiaAI.Business.Componants;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal class SystemDotSpeechSynthesizer : ISynthesizer
    {
        private CultureInfo _culture;

        internal SystemDotSpeechSynthesizer(CultureInfo culture = null)
        {
            _culture = culture ?? new CultureInfo("en-US");
        }

        bool ISynthesizer.IsAvailable() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        Task<bool> ISynthesizer.SpeakAsync(string message)
        {
            bool isSuccessful = false;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                    SpeechEngine.StreamSpeechToSpeaker(audioStream.GetBuffer(), message);
                    isSuccessful = true;
                }
            }

            return Task.FromResult(isSuccessful);
        }
    }
}

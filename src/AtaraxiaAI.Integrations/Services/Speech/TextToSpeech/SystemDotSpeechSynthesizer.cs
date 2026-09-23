using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
{
    internal class SystemDotSpeechSynthesizer : ISynthesizer
    {
        private CultureInfo _culture;

        internal SystemDotSpeechSynthesizer(CultureInfo culture, IntegrationDependencies context)
        {
            _culture = culture ?? new CultureInfo("en-US");
        }

        bool ISynthesizer.IsAvailable() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        Task<byte[]> ISynthesizer.SynthesizeAsync(string message, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                    return Task.FromResult(audioStream.ToArray());
                }
            }

            return Task.FromResult<byte[]>(null);
        }
    }
}

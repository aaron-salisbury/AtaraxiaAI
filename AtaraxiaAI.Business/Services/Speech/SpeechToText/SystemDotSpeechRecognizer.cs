using AtaraxiaAI.Business.Componants;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Recognition;

namespace AtaraxiaAI.Business.Services
{
    public class SystemDotSpeechRecognizer : IRecognizer
    {
        private CultureInfo _culture;
        private SpeechRecognitionEngine _recognizer;

        public SystemDotSpeechRecognizer(CultureInfo culture = null)
        {
            _culture = culture ?? new CultureInfo("en-US");

            BuildRecognizer();
        }

        public bool IsAvailable() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public void Listen(Action<string> speechRecognizedAction)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AI.Log.Logger.Information("Beginning to listen.");

                if (_recognizer == null)
                {
                    BuildRecognizer();
                }

                _recognizer.SpeechRecognized += (s, e) => { speechRecognizedAction(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? e.Result.Text : string.Empty); };

                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        public void Shutdown()
        {
            if (_recognizer != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _recognizer.RecognizeAsyncStop();
                _recognizer.Dispose();
                _recognizer = null;
            }
        }

        private void BuildRecognizer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _recognizer = new SpeechRecognitionEngine(_culture);
                _recognizer.SetInputToDefaultAudioDevice();
                _recognizer.LoadGrammarAsync(GetWakeSkillsGrammar());
            }
        }

        private static Grammar GetWakeSkillsGrammar()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                GrammarBuilder wakeSkills = new GrammarBuilder(OrchestrationEngine.WAKE_COMMAND);

                wakeSkills.Append(new Choices(
                    Enum.GetValues(typeof(OrchestrationEngine.SkillMessages))
                        .Cast<OrchestrationEngine.SkillMessages>()
                        .Select(e => string.Concat(e.ToString().Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' '))
                        .ToArray()));

                return new Grammar(wakeSkills);
            }

            return null;
        }
    }
}

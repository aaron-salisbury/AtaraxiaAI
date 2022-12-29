using AtaraxiaAI.Data;
using AtaraxiaAI.Data.Domains;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Recognition;

namespace AtaraxiaAI.Business.Services
{
    public class SystemDotSpeechRecognizer : IRecognizer
    {
        private SpeechRecognitionEngine _recognizer;

        public SystemDotSpeechRecognizer(CultureInfo culture = null, Grammar recognitionGrammar = null)
        {
            if (IsAvailable())
            {
                culture = culture ?? new CultureInfo("en-US");

                _recognizer = new SpeechRecognitionEngine(culture);
                _recognizer.SetInputToDefaultAudioDevice();
                _recognizer.LoadGrammarAsync(recognitionGrammar ?? GetDefaultGrammar());
            }
        }

        public bool IsAvailable() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public void Listen(Action<string> speechRecognizedAction)
        {
            if (IsAvailable())
            {
                AI.Log.Logger.Information("Beginning to listen.");

                _recognizer.SpeechRecognized += (s, e) => { speechRecognizedAction(e.Result.Text); };

                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        public static Grammar GetDefaultGrammar()
        {
            Choices choices = new Choices();
            List<GrammarChoice> grammarChoices = CRUD.CreateDefaultGrammarChoices(AI.Log.Logger);

            foreach (GrammarChoice choice in grammarChoices.Where(gc => gc.Classification == null))
            {
                choices.Add(choice.Word);
            }

            foreach (GrammarChoice verbChoice in grammarChoices.Where(gc => gc.Classification == GrammarChoice.Classifications.Verb))
            {
                foreach (GrammarChoice placeChoice in grammarChoices.Where(gc => gc.NounType == GrammarChoice.NounTypes.Place))
                {
                    choices.Add($"{verbChoice.Word} {placeChoice.Word}");
                }
            }

            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(choices);

            return new Grammar(gb);
        }
    }
}

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
    public class DotNetSpeechEngine : ISpeechEngine
    {
        const string OS_NOT_IMPLEMENTED_MESSAGE = "This speech engine has only been implemented for the Windows operating system.";

        public SpeechRecognitionEngine Recognizer { get; set; }
        public ISynthesizer Synthesizer { get; set; }

        private CultureInfo _culture;

        public DotNetSpeechEngine(CultureInfo culture = null, Grammar recognitionGrammar = null)
        {
            AI.Log.Logger.Information("Initializing speech engine.");

            _culture = culture ?? new CultureInfo("en-US");

            Recognizer = new SpeechRecognitionEngine(_culture);
            Recognizer.SetInputToDefaultAudioDevice();
            Recognizer.LoadGrammarAsync(recognitionGrammar ?? GetDefaultGrammar());

            SetSynthesizer();
        }

        public void Listen(Action<string> speechRecognizedAction)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AI.Log.Logger.Error(new NotImplementedException(OS_NOT_IMPLEMENTED_MESSAGE), "Failure to start listening.");
            }

            AI.Log.Logger.Information("Beginning to listen.");

            Recognizer.SpeechRecognized += (s, e) => { speechRecognizedAction(e.Result.Text); };

            Recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void Speek(string message)
        {
            Synthesizer.SpeakAsync(message);

            if (!Synthesizer.IsAvailable())
            {
                SetSynthesizer();
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

            ISynthesizer systemDotSpeechSynthesizer = new SystemDotSpeechSynthesizer(_culture);
            if (systemDotSpeechSynthesizer.IsAvailable())
            {
                Synthesizer = systemDotSpeechSynthesizer;
                return;
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

using AtaraxiaAI.Business.Skills;

namespace AtaraxiaAI.Business.Componants
{
    public class OrchestrationEngine
    {
        private SpeechEngine _speechEngine;

        public OrchestrationEngine(SpeechEngine speechEngine)
        {
            _speechEngine = speechEngine;
        }

        public void Heard(string message)
        {
            AI.Log.Logger.Information($"Heard \"{message}\".");

            switch (message.ToLower())
            {
                case "tell me a joke":
                    JokeSkill.TellMeAJoke(_speechEngine);
                    break;
            }
        }
    }
}

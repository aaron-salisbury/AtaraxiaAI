using AtaraxiaAI.Business.Skills;
using System;
using System.Linq;

namespace AtaraxiaAI.Business.Componants
{
    internal class OrchestrationEngine
    {
        public const string WAKE_COMMAND = "Hey Robot";

        internal enum SkillMessages
        {
            TellMeAJoke,
            TellMeADadJoke
        }

        private SpeechEngine _speechEngine;

        internal OrchestrationEngine(SpeechEngine speechEngine)
        {
            _speechEngine = speechEngine;
        }

        internal void Heard(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                AI.Log.Logger.Information($"Heard \"{message}\".");

                if (message.StartsWith(WAKE_COMMAND))
                {
                    int wakeIndex = message.IndexOf(WAKE_COMMAND);
                    string command = message.Remove(wakeIndex, WAKE_COMMAND.Length);
                    string cleanCommand = string.Concat(command.Where(c => !char.IsWhiteSpace(c)));

                    switch ((SkillMessages)Enum.Parse(typeof(SkillMessages), cleanCommand))
                    {
                        case SkillMessages.TellMeAJoke:
                            JokeSkill.TellMeAJoke(_speechEngine);
                            break;
                        case SkillMessages.TellMeADadJoke:
                            JokeSkill.TellMeADadJoke(_speechEngine);
                            break;
                        default:
                            AGISkill.AnswerMe(command, _speechEngine);
                            break;
                    }
                }
            }
        }
    }
}

using AtaraxiaAI.Business.Skills;
using System;
using System.Linq;

namespace AtaraxiaAI.Business.Componants
{
    internal class OrchestrationEngine
    {
        internal const string WAKE_COMMAND = "Hey Robot";

        internal enum SkillMessages
        {
            TellMeAJoke,
            TellMeADadJoke,
            RespondWithInsult
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
                AI.Logger.Information($"*Heard* \"{message}\".");

                if (message.StartsWith(WAKE_COMMAND, StringComparison.OrdinalIgnoreCase))
                {
                    string command = message.Remove(0, WAKE_COMMAND.Length);
                    string cleanCommand = string.Concat(command.Where(c => !char.IsWhiteSpace(c)));

                    switch ((SkillMessages)Enum.Parse(typeof(SkillMessages), cleanCommand, true))
                    {
                        case SkillMessages.TellMeAJoke:
                            JokeSkill.TellMeAJoke(_speechEngine);
                            break;
                        case SkillMessages.TellMeADadJoke:
                            JokeSkill.TellMeADadJoke(_speechEngine);
                            break;
                        case SkillMessages.RespondWithInsult:
                            ResponseSkill.AcquireInsult(_speechEngine);
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

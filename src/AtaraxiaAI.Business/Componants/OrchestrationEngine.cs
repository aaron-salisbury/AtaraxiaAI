using AtaraxiaAI.Business.Skills;
using AtaraxiaAI.Business.Services;
using Serilog;
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
            RespondWithInsult,
            IsMovieStreaming
        }

        private readonly IIntegrationFactory _integrations;
        private readonly ILogger _logger;
        private SpeechEngine _speechEngine;
        private KnowledgeSkill _knowledgeSkill;

        internal OrchestrationEngine(SpeechEngine speechEngine, IIntegrationFactory integrations, ILogger logger)
        {
            _speechEngine = speechEngine;
            _integrations = integrations;
            _logger = logger;
            _knowledgeSkill = new KnowledgeSkill(_speechEngine, integrations);
        }

        internal void Heard(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _logger.Information($"*Heard* \"{message}\".");

                if (message.StartsWith(WAKE_COMMAND, StringComparison.OrdinalIgnoreCase))
                {
                    string command = message.Remove(0, WAKE_COMMAND.Length);
                    string cleanCommand = string.Concat(command.Where(c => !char.IsWhiteSpace(c)));

                    switch ((SkillMessages)Enum.Parse(typeof(SkillMessages), cleanCommand, true))
                    {
                        case SkillMessages.TellMeAJoke:
                            JokeSkill.TellMeAJoke(_speechEngine, _integrations);
                            break;
                        case SkillMessages.TellMeADadJoke:
                            JokeSkill.TellMeADadJoke(_speechEngine, _integrations);
                            break;
                        case SkillMessages.RespondWithInsult:
                            ResponseSkill.AcquireInsult(_speechEngine, _integrations);
                            break;
                        case SkillMessages.IsMovieStreaming:
                            _knowledgeSkill.GetStreamOfferings("Black Adam", true); //TODO: Need a way to communicate the title.
                            break;
                        default:
                            _knowledgeSkill.AnswerMe(command);
                            break;
                    }
                }
            }
        }
    }
}

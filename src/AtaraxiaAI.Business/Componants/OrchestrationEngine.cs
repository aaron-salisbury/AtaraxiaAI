using AtaraxiaAI.Business.Skills;
using AtaraxiaAI.Business.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        internal async Task HeardAsync(string message, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _logger.Information($"*Heard* \"{message}\".");

                if (message.StartsWith(WAKE_COMMAND, StringComparison.OrdinalIgnoreCase))
                {
                    string command = message.Remove(0, WAKE_COMMAND.Length);
                    string cleanCommand = string.Concat(command.Where(char.IsLetter));

                    if (!Enum.TryParse(cleanCommand, true, out SkillMessages skill))
                    {
                        await _knowledgeSkill.AnswerMeAsync(command, cancellationToken);
                        return;
                    }

                    switch (skill)
                    {
                        case SkillMessages.TellMeAJoke:
                            await JokeSkill.TellMeAJokeAsync(_speechEngine, _integrations, cancellationToken);
                            break;
                        case SkillMessages.TellMeADadJoke:
                            await JokeSkill.TellMeADadJokeAsync(_speechEngine, _integrations, cancellationToken);
                            break;
                        case SkillMessages.RespondWithInsult:
                            await ResponseSkill.AcquireInsultAsync(_speechEngine, _integrations, cancellationToken);
                            break;
                        case SkillMessages.IsMovieStreaming:
                            await _knowledgeSkill.GetStreamOfferingsAsync("Black Adam", true, cancellationToken); //TODO: Need a way to communicate the title.
                            break;
                        default:
                            await _knowledgeSkill.AnswerMeAsync(command, cancellationToken);
                            break;
                    }
                }
            }
        }
    }
}

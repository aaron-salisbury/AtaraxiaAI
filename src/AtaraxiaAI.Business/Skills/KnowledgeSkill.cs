using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Skills
{
    internal class KnowledgeSkill
    {
        private SpeechEngine _speechEngine;
        private IAnswerProvider? _answerProvider;
        private IStreamingAvailabilityService _streamingAvailabilityService;

        internal KnowledgeSkill(SpeechEngine speechEngine, IIntegrationFactory integrations)
        {
            _speechEngine = speechEngine;
            _answerProvider = integrations.CreateAnswerProvider();
            _streamingAvailabilityService = integrations.CreateStreamingAvailabilityService();
        }

        internal async Task AnswerMeAsync(string message, CancellationToken cancellationToken)
        {
            if (_answerProvider?.IsAvailable() == true)
            {
                string response = await _answerProvider.AnswerAsync(message);

                if (!string.IsNullOrEmpty(response))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _speechEngine.SpeakAsync(response, cancellationToken);
                }
            }
        }

        internal async Task GetStreamOfferingsAsync(string title, bool isMovie, CancellationToken cancellationToken)
        {
            List<string> offerings;

            if (isMovie)
            {
                offerings = (await _streamingAvailabilityService.GetMovieStreamOfferingsAsync(title))?.ToList();
            }
            else
            {
                offerings = (await _streamingAvailabilityService.GetTVShowStreamOfferingsAsync(title))?.ToList();
            }

            if (offerings != null && offerings.Count > 0)
            {
                StringBuilder sb = new StringBuilder($"{title} is available to stream at ");

                if (offerings.Count == 1)
                {
                    sb.Append($"{offerings.First()}.");
                }
                else
                {
                    sb.Append("the following services: ");

                    for (int i = 0; i < offerings.Count; i++)
                    {
                        if (i < offerings.Count - 1)
                        {
                            sb.Append($"{offerings[i]}, ");
                        }
                        else
                        {
                            sb.Append($"and {offerings[i]}.");
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                await _speechEngine.SpeakAsync(sb.ToString(), cancellationToken);
            }
        }
    }
}

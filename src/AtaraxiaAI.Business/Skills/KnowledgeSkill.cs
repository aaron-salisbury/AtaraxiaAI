using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        internal void AnswerMe(string message)
        {
            if (_answerProvider?.IsAvailable() == true)
            {
                string response = _answerProvider.AnswerAsync(message).Result;

                if (!string.IsNullOrEmpty(response))
                {
                    _speechEngine.Speak(response);
                }
            }
        }

        internal void GetStreamOfferings(string title, bool isMovie)
        {
            List<string> offerings;

            if (isMovie)
            {
                offerings = _streamingAvailabilityService.GetMovieStreamOfferingsAsync(title).Result?.ToList();
            }
            else
            {
                offerings = _streamingAvailabilityService.GetTVShowStreamOfferingsAsync(title).Result?.ToList();
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

                _speechEngine.Speak(sb.ToString());
            }
        }
    }
}

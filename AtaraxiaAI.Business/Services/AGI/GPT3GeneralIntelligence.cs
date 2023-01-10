using AtaraxiaAI.Business.Services.Base.Domains;
using AtaraxiaAI.Data;
using AtaraxiaAI.Data.Base;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    // Inspired by https://learnwithhasan.com/ai-writer-with-open-ai/
    internal class GPT3GeneralIntelligence : IGeneralIntelligence
    {
        private const string API_KEY = null; //TODO: Apply your own key.
        private const string URL_FORMAT = "https://api.openai.com/v1/engines/{0}/completions"; // {0}Engine
        private const string ENGINE = "text-davinci-003";
        private const double TEMPERATURE = 0.7d;
        private const int TOP_P = 1;
        private const int FREQ_PENALTY = 0;
        private const int PRESENCE_PENALTY = 0;

        private int _tokens;

        internal GPT3GeneralIntelligence(int tokens = 256)
        {
            _tokens = tokens;
        }

        bool IGeneralIntelligence.IsAvailable() => !string.IsNullOrEmpty(API_KEY);

        async Task<string> IGeneralIntelligence.AnswerMe(string message)
        {
            string response = null;

            string content = 
                $"{{\n  \"prompt\": \"{message}\",\n  \"temperature\": {TEMPERATURE}," + 
                $"\n  \"max_tokens\": {_tokens},\n  \"top_p\": {TOP_P}," + 
                $"\n  \"frequency_penalty\": {FREQ_PENALTY},\n  \"presence_penalty\": {PRESENCE_PENALTY}\n}}";

            string json = await WebRequests.SendHTTPJsonRequestAsync(
                string.Format(URL_FORMAT, ENGINE), 
                AI.HttpClientFactory, 
                AI.Logger,
                content: content,
                requestHeaders: new Dictionary<string, string>() { { "Authorization", $"Bearer {API_KEY}" } },
                httpMethod: "POST");

            if (!string.IsNullOrEmpty(json))
            {
                GPT3Root gPT3Root = JsonSerializer.Deserialize<GPT3Root>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (gPT3Root != null && gPT3Root.Choices.Count > 0)
                {
                    response = gPT3Root.Choices.First().Text;
                }
            }

            return response;
        }
    }
}

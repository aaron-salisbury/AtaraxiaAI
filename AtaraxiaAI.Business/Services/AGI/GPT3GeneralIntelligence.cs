using AtaraxiaAI.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    // Inspired by https://learnwithhasan.com/ai-writer-with-open-ai/
    internal class GPT3GeneralIntelligence : IGeneralIntelligence
    {
        private const string API_KEY = "API_KEY"; //TODO: Apply your own key.
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

        bool IGeneralIntelligence.IsAvailable() => false; //TODO: Flip when using real API key.

        async Task<string> IGeneralIntelligence.AnswerMe(string message)
        {
            string response = null;

            StringContent content = new StringContent(
                $"{{\n  \"prompt\": \"{message}\",\n  \"temperature\": {TEMPERATURE}," + 
                $"\n  \"max_tokens\": {_tokens},\n  \"top_p\": {TOP_P}," + 
                $"\n  \"frequency_penalty\": {FREQ_PENALTY},\n  \"presence_penalty\": {PRESENCE_PENALTY}\n}}");

            string json = await WebRequests.SendPOSTAsync(
                string.Format(URL_FORMAT, ENGINE),
                AI.Log.Logger,
                content,
                new Dictionary<string, string>() { { "Authorization", $"Bearer {API_KEY}" } });

            if (!string.IsNullOrEmpty(json))
            {
                //TODO: Deserialize to actual domain.
                dynamic dynObj = JsonConvert.DeserializeObject(json);
                if (dynObj != null)
                {
                    response = dynObj.choices[0].text.ToString();
                }
            }

            return response;
        }
    }
}

using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Skills
{
    // Inspired by https://learnwithhasan.com/ai-writer-with-open-ai/
    public static class GPT3Skill
    {
        private const string API_KEY = "API_KEY"; //TODO: Apply your own key.
        private const string URL_FORMAT = "https://api.openai.com/v1/engines/{0}/completions"; // {0}Engine
        private const string ENGINE = "text-davinci-003";
        private const double TEMPERATURE = 0.7d;
        private const int TOP_P = 1;
        private const int FREQ_PENALTY = 0;
        private const int PRESENCE_PENALTY = 0;

        public static bool IsAvailable() => false; //TODO: Flip when using real API key.

        public static async Task AnswerMe(string message, SpeechEngine speechEngine, int tokens = 256)
        {
            StringContent content = new StringContent(
                $"{{\n  \"prompt\": \"{message}\",\n  \"temperature\": {TEMPERATURE}," + 
                $"\n  \"max_tokens\": {tokens},\n  \"top_p\": {TOP_P}," + 
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
                    speechEngine.Speak(dynObj.choices[0].text.ToString());
                }
            }
        }
    }
}

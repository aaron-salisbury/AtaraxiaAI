using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;

namespace AtaraxiaAI.Business.Skills
{
    internal static class AGISkill
    {
        internal static void AnswerMe(string message, SpeechEngine speechEngine)
        {
            IGeneralIntelligence aGI = new GPT3GeneralIntelligence();

            if (aGI.IsAvailable())
            {
                string response = aGI.AnswerMe(message).Result;

                if (!string.IsNullOrEmpty(response))
                {
                    speechEngine.Speak(response);
                }
            }
        }
    }
}

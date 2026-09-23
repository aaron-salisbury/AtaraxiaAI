using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;

namespace AtaraxiaAI.Business.Skills
{
    internal static class ResponseSkill
    {
        internal static void AcquireInsult(SpeechEngine speechEngine)
        {
            IInsultService insultService = AI.Integrations.CreateInsultService();

            string insult = insultService.GetInsultAsync().Result;

            if (!string.IsNullOrEmpty(insult))
            {
                speechEngine.Speak(insult);
            }
        }
    }
}

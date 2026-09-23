using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;

namespace AtaraxiaAI.Business.Skills
{
    internal static class ResponseSkill
    {
        internal static void AcquireInsult(SpeechEngine speechEngine, IIntegrationFactory integrations)
        {
            IInsultService insultService = integrations.CreateInsultService();

            string insult = insultService.GetInsultAsync().Result;

            if (!string.IsNullOrEmpty(insult))
            {
                speechEngine.Speak(insult);
            }
        }
    }
}

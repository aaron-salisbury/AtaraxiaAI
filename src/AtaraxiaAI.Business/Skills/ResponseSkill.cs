using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Skills
{
    internal static class ResponseSkill
    {
        internal static async Task AcquireInsultAsync(SpeechEngine speechEngine, IIntegrationFactory integrations, CancellationToken cancellationToken)
        {
            IInsultService insultService = integrations.CreateInsultService();
            string insult = await insultService.GetInsultAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(insult)) await speechEngine.SpeakAsync(insult, cancellationToken);
        }
    }
}

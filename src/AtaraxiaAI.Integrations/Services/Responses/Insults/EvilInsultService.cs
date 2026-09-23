using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Integrations.DTOs;
using RunnethOverStudio.AppToolkit.Modules.Access;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
{
    // https://evilinsult.com/api/
    internal class EvilInsultService : IInsultService
    {
        private const string URL_FORMAT = "https://evilinsult.com/generate_insult.php?lang={0}&type=json"; // {0}Language

        async Task<string> IInsultService.GetInsultAsync()
        {
            AI.Logger.Information("Acquiring insult.");

            string insult = null;

            string url = string.Format(URL_FORMAT, "en");

            string json = await AI.HttpRequester.SendHTTPJsonRequestAsync(url);

            if (!string.IsNullOrEmpty(json))
            {
                EvilInsult evilInsult = JsonSerializer.Deserialize<EvilInsult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!string.IsNullOrEmpty(evilInsult?.Insult))
                {
                    insult = evilInsult.Insult;
                }
            }

            if (string.IsNullOrEmpty(insult))
            {
                AI.Logger.Error("Failed to aquire insult.");
            }

            return insult;
        }
    }
}

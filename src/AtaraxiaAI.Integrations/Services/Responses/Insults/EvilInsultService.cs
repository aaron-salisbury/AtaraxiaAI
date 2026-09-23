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
        private readonly IntegrationDependencies _dependencies;

        internal EvilInsultService(IntegrationDependencies dependencies) => _dependencies = dependencies;
        private const string URL_FORMAT = "https://evilinsult.com/generate_insult.php?lang={0}&type=json"; // {0}Language

        async Task<string> IInsultService.GetInsultAsync()
        {
            _dependencies.Logger.Information("Acquiring insult.");

            string insult = null;

            string url = string.Format(URL_FORMAT, "en");

            string json = await _dependencies.HttpRequester.SendHTTPJsonRequestAsync(url);

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
                _dependencies.Logger.Error("Failed to aquire insult.");
            }

            return insult;
        }
    }
}

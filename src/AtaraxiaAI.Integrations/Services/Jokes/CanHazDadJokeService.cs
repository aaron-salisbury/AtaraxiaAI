using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using AtaraxiaAI.Integrations.DTOs;
using RunnethOverStudio.AppToolkit.Modules.Access;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
{
    // https://icanhazdadjoke.com/api
    internal class CanHazDadJokeService : IJokeService
    {
        private readonly IntegrationDependencies _dependencies;
        private const string URL = "https://icanhazdadjoke.com/";

        private string _userAgent;

        internal CanHazDadJokeService(IntegrationDependencies dependencies, string userAgent = null)
        {
            _dependencies = dependencies;
            _userAgent = userAgent ?? "AtaraxiaAI (compatible; https://github.com/aaron-salisbury/AtaraxiaAI)";
        }

        async Task<Joke> IJokeService.GetJokeAsync()
        {
            _dependencies.Logger.Information("Acquiring dad joke.");

            Joke joke = null;

            string json = await _dependencies.HttpRequester.SendHTTPJsonRequestAsync(URL, new HTTPJsonRequest { UserAgent = _userAgent });

            if (!string.IsNullOrEmpty(json))
            {
                CanHazDadJoke canHazDadJoke = JsonSerializer.Deserialize<CanHazDadJoke>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (canHazDadJoke != null)
                {
                    joke = new Joke { JokeType = Joke.JokeTypes.Single, JokeLine = canHazDadJoke.Joke };
                }
            }

            if (joke == null)
            {
                _dependencies.Logger.Error("Failed to aquire joke.");
            }

            return joke;
        }
    }
}

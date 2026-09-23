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
        private const string URL = "https://icanhazdadjoke.com/";

        private string _userAgent;

        internal CanHazDadJokeService(string userAgent = null)
        {
            _userAgent = userAgent ?? "AtaraxiaAI (compatible; https://github.com/aaron-salisbury/AtaraxiaAI)";
        }

        async Task<Joke> IJokeService.GetJokeAsync()
        {
            AI.Logger.Information("Acquiring dad joke.");

            Joke joke = null;

            string json = await AI.HttpRequester.SendHTTPJsonRequestAsync(URL, new HTTPJsonRequest { UserAgent = _userAgent });

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
                AI.Logger.Error("Failed to aquire joke.");
            }

            return joke;
        }
    }
}

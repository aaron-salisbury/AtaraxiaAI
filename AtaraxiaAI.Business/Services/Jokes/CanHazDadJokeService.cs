using AtaraxiaAI.Business.Services.Base.Domains;
using AtaraxiaAI.Business.Services.Base.Models;
using AtaraxiaAI.Data;
using AtaraxiaAI.Data.Base;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
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

            string json = await WebRequests.SendHTTPJsonRequestAsync(URL, AI.HttpClientFactory, AI.Logger, userAgent: _userAgent);

            if (!string.IsNullOrEmpty(json))
            {
                CanHazDadJoke canHazDadJoke = JsonSerializer.Deserialize<CanHazDadJoke>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (canHazDadJoke != null)
                {
                    joke = Joke.ConvertFromCanHazDadJoke(canHazDadJoke);
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

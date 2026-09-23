using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using AtaraxiaAI.Integrations.DTOs;
using RunnethOverStudio.AppToolkit.Modules.Access;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
{
    // https://sv443.net/jokeapi/v2/
    internal class Sv443JokeService : IJokeService
    {
        private const string URL_FORMAT = "https://v2.jokeapi.dev/joke/{0}"; // {0}Categories

        internal enum CategoryTypes
        {
            Any,
            Programming,
            Miscellaneous,
            Dark,
            Pun,
            Spooky,
            Christmas
        }

        internal enum JokeTypes
        {
            Any,
            Single,
            TwoPart
        }

        internal IEnumerable<CategoryTypes> Categories { get; set; }
        internal Sv443JokeFlags Flags { get; set; }
        internal JokeTypes JokeType { get; set; }

        internal Sv443JokeService(IEnumerable<CategoryTypes> categories = null, Sv443JokeFlags flags = null, JokeTypes jokeType = JokeTypes.Any)
        {
            Categories = categories;
            Flags = flags;
            JokeType = jokeType;
        }

        async Task<Joke> IJokeService.GetJokeAsync()
        {
            AI.Logger.Information("Acquiring joke.");

            Joke joke = null;

            Categories = Categories ?? new CategoryTypes[] { CategoryTypes.Any };
            string categoryParams = string.Join(",", Categories);

            Flags = Flags ?? Sv443JokeFlags.BuildSafeFlags();
            string blacklistParams = Flags.GetBlacklistParams();

            string url = string.Format(URL_FORMAT, categoryParams);

            if (!string.IsNullOrEmpty(blacklistParams))
            {
                url += $"?blacklistFlags={blacklistParams}";
            }

            if (JokeType != JokeTypes.Any)
            {
                url += $"?type={JokeType.ToString().ToLower()}";
            }

            string json = await AI.HttpRequester.SendHTTPJsonRequestAsync(url);

            if (!string.IsNullOrEmpty(json))
            {
                Sv443Joke sv443Joke = JsonSerializer.Deserialize<Sv443Joke>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (sv443Joke != null)
                {
                    joke = new Joke
                    {
                        JokeType = (Joke.JokeTypes)System.Enum.Parse(typeof(Joke.JokeTypes), sv443Joke.Type, true),
                        JokeLine = sv443Joke.Joke,
                        Setup = sv443Joke.Setup,
                        Delivery = sv443Joke.Delivery
                    };
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

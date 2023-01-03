using AtaraxiaAI.Business.Services.Base.Domains;
using AtaraxiaAI.Business.Services.Base.Models;
using AtaraxiaAI.Data;
using AtaraxiaAI.Data.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
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
            AI.Log.Logger.Information("Acquiring joke.");

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

            string json = await WebRequests.SendGETAsync(url, AI.Log.Logger);

            if (!string.IsNullOrEmpty(json))
            {
                Sv443Joke sv443Joke = await Json.ToObjectAsync<Sv443Joke>(json);

                if (sv443Joke != null)
                {
                    joke = Joke.ConvertFromSv443Joke(sv443Joke);
                }
            }

            if (joke == null)
            {
                AI.Log.Logger.Error("Failed to aquire joke.");
            }

            return joke;
        }
    }
}

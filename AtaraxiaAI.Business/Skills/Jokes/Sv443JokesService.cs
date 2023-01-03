using AtaraxiaAI.Business.Skills.Domains;
using AtaraxiaAI.Data;
using AtaraxiaAI.Data.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Skills
{
    // https://sv443.net/jokeapi/v2/
    public static class Sv443JokesService
    {
        private const string URL_FORMAT = "https://v2.jokeapi.dev/joke/{0}"; // {0}Categories

        public enum Categories
        {
            Any,
            Programming,
            Miscellaneous,
            Dark,
            Pun,
            Spooky,
            Christmas
        }

        public enum Types
        {
            Any,
            Single,
            TwoPart
        }

        public static async Task<Sv443Joke> GetJokeAsync(IEnumerable<Categories> categories = null, Sv443JokeFlags flags = null, Types jokeType = Types.Any)
        {
            AI.Log.Logger.Information("Acquiring joke.");

            Sv443Joke joke = null;

            categories = categories ?? new Categories[] { Categories.Any };
            string categoryParams = string.Join(",", categories);

            flags = flags ?? Sv443JokeFlags.BuildSafeFlags();
            string blacklistParams = flags.GetBlacklistParams();

            string url = string.Format(URL_FORMAT, categoryParams);

            if (!string.IsNullOrEmpty(blacklistParams))
            {
                url += $"?blacklistFlags={blacklistParams}";
            }

            if (jokeType != Types.Any)
            {
                url += $"?type={jokeType.ToString().ToLower()}";
            }

            string json = await WebRequests.SendGETAsync(url, AI.Log.Logger);

            if (!string.IsNullOrEmpty(json))
            {
                joke = await Json.ToObjectAsync<Sv443Joke>(json);
            }

            if (joke == null)
            {
                AI.Log.Logger.Error("Failed to aquire joke.");
            }

            return joke;
        }
    }
}

using AtaraxiaAI.Business.Services.Base.Domains;

namespace AtaraxiaAI.Business.Services.Base.Models
{
    internal class Joke
    {
        public enum JokeTypes
        {
            Single,
            TwoPart
        }

        internal JokeTypes JokeType { get; set; }
        internal string JokeLine { get; set; }
        internal string Setup { get; set; }
        internal string Delivery { get; set; }

        internal static Joke ConvertFromSv443Joke(Sv443Joke sv443Joke)
        {
            return new Joke
            {
                JokeType = (JokeTypes)System.Enum.Parse(typeof(JokeTypes), sv443Joke.Type, true),
                JokeLine = sv443Joke.Joke,
                Setup = sv443Joke.Setup,
                Delivery = sv443Joke.Delivery
            };
        }
    }
}

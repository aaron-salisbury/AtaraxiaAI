using AtaraxiaAI.Business.Services.Base.DTOs;

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

        internal static Joke ConvertFromCanHazDadJoke(CanHazDadJoke canHazDadJoke)
        {
            return new Joke
            {
                JokeType = JokeTypes.Single,
                JokeLine = canHazDadJoke.Joke
            };
        }
    }
}

namespace AtaraxiaAI.Business.Services.Base.Models
{
    public class Joke
    {
        public enum JokeTypes { Single, TwoPart }
        internal JokeTypes JokeType { get; set; }
        internal string JokeLine { get; set; }
        internal string Setup { get; set; }
        internal string Delivery { get; set; }
    }
}

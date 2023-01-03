using System;

namespace AtaraxiaAI.Business.Services.Base.Domains
{
    [Serializable]
    internal class Sv443Joke
    {
        public bool Error { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public string Joke { get; set; }
        public string Setup { get; set; }
        public string Delivery { get; set; }
        public Sv443JokeFlags Flags { get; set; }
        public int ID { get; set; }
        public bool Safe { get; set; }
        public string Lang { get; set; }
    }
}

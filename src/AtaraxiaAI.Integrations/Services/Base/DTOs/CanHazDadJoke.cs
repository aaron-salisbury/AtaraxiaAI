using System;

namespace AtaraxiaAI.Integrations.DTOs
{
    [Serializable]
    internal class CanHazDadJoke
    {
        public string ID { get; set; }
        public string Joke { get; set; }
        public int Status { get; set; }
    }
}

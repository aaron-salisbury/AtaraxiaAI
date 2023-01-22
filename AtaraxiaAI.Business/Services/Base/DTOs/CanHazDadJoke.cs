using System;

namespace AtaraxiaAI.Business.Services.Base.DTOs
{
    [Serializable]
    internal class CanHazDadJoke
    {
        public string ID { get; set; }
        public string Joke { get; set; }
        public int Status { get; set; }
    }
}

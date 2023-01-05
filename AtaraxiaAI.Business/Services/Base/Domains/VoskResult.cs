using System;

namespace AtaraxiaAI.Business.Services.Base.Domains
{
    [Serializable]
    internal class VoskResult
    {
        public double Conf { get; set; }
        public double End { get; set; }
        public double Start { get; set; }
        public string Word { get; set; }
    }
}

using System;

namespace AtaraxiaAI.Business.Services.Base.Domains
{
    [Serializable]
    internal class WatchmodeSource
    {
        public int Source_Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Region { get; set; }
        public string Ios_Url { get; set; }
        public string Android_Url { get; set; }
        public string Web_Url { get; set; }
        public string Format { get; set; }
        public double? Price { get; set; }
        public object Seasons { get; set; }
        public object Episodes { get; set; }
    }
}

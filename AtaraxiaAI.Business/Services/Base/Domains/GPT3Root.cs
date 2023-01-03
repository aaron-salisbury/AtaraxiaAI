using System;
using System.Collections.Generic;

namespace AtaraxiaAI.Business.Services.Base.Domains
{
    [Serializable]
    internal class GPT3Root
    {
        public string ID { get; set; }
        public string @Object { get; set; }
        public int Created { get; set; }
        public string Model { get; set; }
        public List<GPT3Choice> Choices { get; set; }
        public GPT3Usage Usage { get; set; }
    }
}

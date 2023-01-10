using System;

namespace AtaraxiaAI.Business.Services.Base.Domains
{
    [Serializable]
    internal class EvilInsult
    {
        public string Number { get; set; }
        public string Language { get; set; }
        public string Insult { get; set; }
        public string Created { get; set; }
        public string Shown { get; set; }
        public string Createdby { get; set; }
        public string Active { get; set; }
        public string Comment { get; set; }
    }
}

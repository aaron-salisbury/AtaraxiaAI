using System;

namespace AtaraxiaAI.Business.Services.Base.DTOs
{
    [Serializable]
    internal class GPT3Choice
    {
        public string Text { get; set; }
        public int Index { get; set; }
        public object Logprobs { get; set; }
        public string Finish_Reason { get; set; }
    }
}

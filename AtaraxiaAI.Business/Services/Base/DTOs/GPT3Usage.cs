using System;

namespace AtaraxiaAI.Business.Services.Base.DTOs
{
    [Serializable]
    internal class GPT3Usage
    {
        public int Prompt_Tokens { get; set; }
        public int Completion_Tokens { get; set; }
        public int Total_Tokens { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace AtaraxiaAI.Business.Services.Base.DTOs
{
    [Serializable]
    internal class BingVoiceTag
    {
        public List<string> ContentCategories { get; set; }
        public List<string> VoicePersonalities { get; set; }
    }
}

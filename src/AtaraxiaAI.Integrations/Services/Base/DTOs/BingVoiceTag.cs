using System;
using System.Collections.Generic;

namespace AtaraxiaAI.Integrations.DTOs
{
    [Serializable]
    internal class BingVoiceTag
    {
        public List<string> ContentCategories { get; set; }
        public List<string> VoicePersonalities { get; set; }
    }
}

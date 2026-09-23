using System;
using System.Collections.Generic;

namespace AtaraxiaAI.Integrations.DTOs
{
    [Serializable]
    internal class VoskResultRoot
    {
        public List<VoskResult> Result { get; set; }
        public string Text { get; set; }
    }
}

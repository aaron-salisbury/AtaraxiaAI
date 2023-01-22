using System;
using System.Collections.Generic;

namespace AtaraxiaAI.Business.Services.Base.DTOs
{
    [Serializable]
    internal class VoskRoot
    {
        public List<VoskResult> Result { get; set; }
        public string Text { get; set; }
    }
}

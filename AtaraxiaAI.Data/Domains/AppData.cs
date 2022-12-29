using System;

namespace AtaraxiaAI.Data.Domains
{
    [Serializable]
    public class AppData
    {
        public int MonthOfLastCloudServicesRoll { get; set; }
        public int GoogleCloudSpeechToTextByteCount { get; set; }
    }
}

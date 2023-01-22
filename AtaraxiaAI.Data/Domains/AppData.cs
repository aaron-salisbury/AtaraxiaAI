using System;

namespace AtaraxiaAI.Data.Domains
{
    [Serializable]
    public class AppData
    {
        public int MonthOfLastCloudServicesRoll { get; set; }
        public int MicrosoftAzureSpeechToTextCharCount { get; set; }
        public int GoogleCloudSpeechToTextByteCount { get; set; }
        public int WatchmodeCurrentAPIUsage { get; set; }
        public DateTime? WatchmodeQuotaResetsOn { get; set; }
    }
}

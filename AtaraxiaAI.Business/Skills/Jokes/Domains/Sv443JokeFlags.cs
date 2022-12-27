using System;
using System.Collections.Generic;

namespace AtaraxiaAI.Business.Skills
{
    [Serializable]
    public class Sv443JokeFlags
    {
        public bool NSFW { get; set; }
        public bool Religious { get; set; }
        public bool Political { get; set; }
        public bool Racist { get; set; }
        public bool Sexist { get; set; }
        public bool Explicit { get; set; }

        public string GetBlacklistParams()
        {
            List<string> bps = new List<string>();

            if (NSFW) { bps.Add("nsfw"); }
            if (Religious) { bps.Add("religious"); }
            if (Political) { bps.Add("political"); }
            if (Racist) { bps.Add("racist"); }
            if (Sexist) { bps.Add("sexist"); }
            if (Explicit) { bps.Add("explicit"); }

            return string.Join(",", bps);
        }

        public static Sv443JokeFlags BuildSafeFlags()
        {
            return new Sv443JokeFlags
            {
                NSFW = true,
                Religious = true,
                Political = true,
                Racist = true,
                Sexist = true,
                Explicit = true
            };
        }
    }
}

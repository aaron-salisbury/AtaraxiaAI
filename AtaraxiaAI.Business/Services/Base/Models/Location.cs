using System;

namespace AtaraxiaAI.Business.Services.Base.Models
{
    internal class Location
    {
        public string City { get; set; }
        public string Region { get; set; }
        public string Zip { get; set; }

        public override string ToString()
        {
            return $"Location: {City}, {Region} {Zip}";
        }
    }
}

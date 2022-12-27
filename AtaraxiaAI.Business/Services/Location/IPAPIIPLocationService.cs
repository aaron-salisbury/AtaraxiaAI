using AtaraxiaAI.Data;
using AtaraxiaAI.Data.Base;
using AtaraxiaAI.Data.Domains;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public class IPAPIIPLocationService : IIPLocationService
    {
        private const string URL_FORMAT = "http://ip-api.com/json/{0}"; // {0}IP Address

        public async Task<Location> GetLocationByIPAsync(string iPAddress)
        {
            if (!string.IsNullOrEmpty(iPAddress))
            {
                string url = string.Format(URL_FORMAT, iPAddress);
                string json = await WebRequests.GetCurlResponseAsync(url, AI.Log.Logger);

                if (!string.IsNullOrEmpty(json))
                {
                    Location location = await Json.ToObjectAsync<Location>(json);

                    if (location != null)
                    {
                        AI.Log.Logger.Information($"Approximate Location: {location.City}, {location.Region} {location.Zip}");
                        return location;
                    }
                    else
                    {
                        AI.Log.Logger.Error("Failed to determine location.");
                    }
                }
            }

            return null;
        }
    }
}

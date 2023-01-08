using AtaraxiaAI.Business.Services.Base.Domains;
using AtaraxiaAI.Business.Services.Base.Models;
using AtaraxiaAI.Data;
using AtaraxiaAI.Data.Base;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal class IPAPIIPLocationService : IIPLocationService
    {
        private const string URL_FORMAT = "http://ip-api.com/json/{0}"; // {0}IP Address

        async Task<Location> IIPLocationService.GetLocationByIPAsync(string iPAddress)
        {
            if (!string.IsNullOrEmpty(iPAddress))
            {
                string url = string.Format(URL_FORMAT, iPAddress);
                string json = await WebRequests.SendHTTPJsonRequestAsync(url, AI.Log.Logger);

                if (!string.IsNullOrEmpty(json))
                {
                    IPAPILocation iPAPILocation = JsonSerializer.Deserialize<IPAPILocation>(json);

                    if (iPAPILocation != null)
                    {
                        return new Location
                        {
                            City = iPAPILocation.City,
                            Region = iPAPILocation.Region,
                            Zip = iPAPILocation.Zip
                        };
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

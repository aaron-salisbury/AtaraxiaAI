using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using AtaraxiaAI.Integrations.DTOs;
using RunnethOverStudio.AppToolkit.Modules.Access;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
{
    internal class IPAPIIPLocationService : IIPLocationService
    {
        private const string URL_FORMAT = "http://ip-api.com/json/{0}"; // {0}IP Address

        async Task<Location> IIPLocationService.GetLocationByIPAsync(string iPAddress)
        {
            if (!string.IsNullOrEmpty(iPAddress))
            {
                string url = string.Format(URL_FORMAT, iPAddress);
                string json = await AI.HttpRequester.SendHTTPJsonRequestAsync(url);

                if (!string.IsNullOrEmpty(json))
                {
                    IPAPILocation iPAPILocation = JsonSerializer.Deserialize<IPAPILocation>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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
                        AI.Logger.Error("Failed to determine location.");
                    }
                }
            }

            return null;
        }
    }
}

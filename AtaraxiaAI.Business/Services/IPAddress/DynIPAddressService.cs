using System.IO;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal class DynIPAddressService : IIPAddressService
    {
        private const string REQUEST_URL = "http://checkip.dyndns.org/";

        async Task<string> IIPAddressService.GetPublicIPAddressAsync()
        {
            AI.Logger.Information("Determining IP Address.");

            string ip = null;

            using (StreamReader stream = new StreamReader(
                await Data.WebRequests.GetWebRequestStreamAsync(REQUEST_URL, AI.HttpClientFactory, AI.Logger)))
            {
                string response = stream.ReadToEnd();

                if (!string.IsNullOrEmpty(response))
                {
                    int first = response.IndexOf("Address: ") + 9;
                    int last = response.LastIndexOf("</body>");
                    ip = response.Substring(first, last - first);

                    AI.Logger.Information($"IP Address: {ip}");
                }
                else
                {
                    AI.Logger.Error("Failed to determine IP Address.");
                }
            }

            return ip;
        }
    }
}

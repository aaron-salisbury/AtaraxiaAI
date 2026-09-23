using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Services;
using System.IO;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
{
    internal class IPIFYIPAddressService : IIPAddressService
    {
        private const string REQUEST_URL = "https://api.ipify.org";

        async Task<string> IIPAddressService.GetPublicIPAddressAsync()
        {
            string ip = null;

            using (StreamReader stream = new StreamReader(new MemoryStream(
                await AI.HttpRequester.GetWebRequestSerializedAsync(REQUEST_URL))))
            {
                string response = stream.ReadToEnd();

                if (!string.IsNullOrEmpty(response))
                {
                    ip = response;
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

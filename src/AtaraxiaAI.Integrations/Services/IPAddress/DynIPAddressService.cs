using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Services;
using System.IO;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
{
    internal class DynIPAddressService : IIPAddressService
    {
        private readonly IntegrationDependencies _dependencies;

        internal DynIPAddressService(IntegrationDependencies dependencies) => _dependencies = dependencies;
        private const string REQUEST_URL = "http://checkip.dyndns.org/";

        async Task<string> IIPAddressService.GetPublicIPAddressAsync()
        {
            _dependencies.Logger.Information("Determining IP Address.");

            string ip = null;

            using (StreamReader stream = new StreamReader(new MemoryStream(
                await _dependencies.HttpRequester.GetWebRequestSerializedAsync(REQUEST_URL))))
            {
                string response = stream.ReadToEnd();

                if (!string.IsNullOrEmpty(response))
                {
                    int first = response.IndexOf("Address: ") + 9;
                    int last = response.LastIndexOf("</body>");
                    ip = response.Substring(first, last - first);

                    _dependencies.Logger.Information($"IP Address: {ip}");
                }
                else
                {
                    _dependencies.Logger.Error("Failed to determine IP Address.");
                }
            }

            return ip;
        }
    }
}

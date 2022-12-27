﻿using System.IO;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal class IPIFYIPAddressService : IIPAddressService
    {
        private const string REQUEST_URL = "https://api.ipify.org";

        public async Task<string> GetPublicIPAddressAsync()
        {
            AI.Log.Logger.Information("Determining IP Address.");

            string ip = null;

            using (StreamReader stream = new StreamReader(
                await Data.WebRequests.GetWebRequestStreamAsync(REQUEST_URL, AI.Log.Logger)))
            {
                string response = stream.ReadToEnd();

                if (!string.IsNullOrEmpty(response))
                {
                    ip = response;
                }
                else
                {
                    AI.Log.Logger.Information("Failed to determine IP Address.");
                }
            }

            return ip;
        }
    }
}
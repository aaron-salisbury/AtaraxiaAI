using AtaraxiaAI.Data.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AtaraxiaAI.Data
{
    public static class WebRequests
    {
        public static async Task<string?> SendHTTPJsonRequestAsync(string url, IHttpClientFactory httpClientFactory, ILogger logger, string? content = null, Dictionary<string, string>? requestHeaders = null, string httpMethod = "GET", string? userAgent = null)
        {
            try
            {
                using (HttpClient client = httpClientFactory.CreateClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    if (userAgent != null)
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                    }

                    if (requestHeaders != null)
                    {
                        foreach (KeyValuePair<string, string> valueByName in requestHeaders)
                        {
                            client.DefaultRequestHeaders.TryAddWithoutValidation(valueByName.Key, valueByName.Value);
                        }
                    }

                    if (string.IsNullOrEmpty(content) && string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        return await client.GetStringAsync(url);
                    }

                    using (HttpRequestMessage request = new(new HttpMethod(httpMethod), url))
                    {
                        if (!string.IsNullOrEmpty(content))
                        {
                            request.Content = new StringContent(content);
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                        }

                        HttpResponseMessage response = await client.SendAsync(request);
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"HTTP request failed: {e.Message}");
                return null;
            }
        }

        public static async Task DownloadFileAsync(IHttpClientFactory httpClientFactory, string url, string fullFilePath, bool deletePreexisting = false)
        {
            if (deletePreexisting && File.Exists(fullFilePath))
            {
                File.Delete(fullFilePath);
            }

            using (HttpClient client = httpClientFactory.CreateClient())
            {
                await client.DownloadFileTaskAsync(new Uri(url), fullFilePath);
            }
        }

        public static async Task<Stream> GetWebRequestStreamAsync(string url, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            HttpResponseMessage? response = await GetWebRequestResponseAsync(url, httpClientFactory, logger);

            if (response != null)
            {
                return await response.Content.ReadAsStreamAsync();

            }

            return new MemoryStream();
        }

        public static async Task<byte[]> GetWebRequestSerializedAsync(string url, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            HttpResponseMessage? response = await GetWebRequestResponseAsync(url, httpClientFactory, logger);

            if (response != null)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return Array.Empty<byte>();

        }

        private static async Task<HttpResponseMessage?> GetWebRequestResponseAsync(string url, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            try
            {
                using (HttpClient client = httpClientFactory.CreateClient())
                {
                    return await client.GetAsync(url);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Web request failed: {e.Message}");
                return null;
            }
        }
    }
}

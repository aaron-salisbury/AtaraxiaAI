using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AtaraxiaAI.Data
{
    public class WebRequests
    {
        public static async Task<string> SendHTTPJsonRequestAsync(string url, ILogger logger, StringContent content = null, Dictionary<string, string> requestHeaders = null, string httpMethod = "GET", string userAgent = null)
        {
            try
            {
                using (HttpClient client = new HttpClient())
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

                    if (content == null && string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        return await client.GetStringAsync(url);
                    }

                    using (HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(httpMethod), url))
                    {
                        if (content != null)
                        {
                            request.Content = content;
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

        public static async Task<Stream> GetWebRequestStreamAsync(string url, ILogger logger)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                WebResponse response = await request.GetResponseAsync();

                return response.GetResponseStream();
            }
            catch (Exception e)
            {
                logger.Error($"Web request request failed: {e.Message}");
                return null;
            }
        }
    }
}

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AtaraxiaAI.Data.Base
{
    public static class HttpClientExtensions
    {
        public static async Task DownloadFileTaskAsync(this HttpClient client, Uri address, string fileName)
        {
            using (Stream stream = await client.GetStreamAsync(address))
            using (FileStream fileStream = new FileStream(fileName, FileMode.CreateNew))
            {
                await stream.CopyToAsync(fileStream);
            }
        }
    }
}

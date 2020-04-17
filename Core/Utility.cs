using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NosAyudamos
{
    public static class Utility
    {
        public async static Task<byte[]> DownloadBlobAsync(Uri blobUri)
        {
            using var client = new HttpClient();
            return await client.GetByteArrayAsync(blobUri);
        }
    }
}
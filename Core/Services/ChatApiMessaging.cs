using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NosAyudamos
{
    [NoExport]
    class ChatApiMessaging : IMessaging
    {
        readonly HttpClient httpClient;
        readonly ISerializer serializer;
        readonly IEnvironment environment;

        public ChatApiMessaging(IEnvironment environment, HttpClient httpClient, ISerializer serializer)
        {
            this.httpClient = httpClient;
            this.serializer = serializer;
            this.environment = environment;
        }

        public async Task SendTextAsync(string from, string body, string to)
        {
            var baseUrl = environment.GetVariable("ChatApiBaseUrl").TrimEnd('/');
            var token = environment.GetVariable("ChatApiToken");

            // Markdown-like image format to allow for text + url
            if (body.StartsWith("![", StringComparison.Ordinal))
            {
                var text = body.Substring(2, body.LastIndexOf(']') - 2);
                var url = body.Substring(body.LastIndexOf('(')).TrimStart('(').TrimEnd(')');

                // Post as file.
                var bytes = await httpClient.GetByteArrayAsync(url);
                var msg = new
                {
                    phone = to.TrimStart('+'),
                    body = "data:image/gif;base64," + Convert.ToBase64String(bytes),
                    filename = "captcha.gif",
                    caption = text,
                };

                using var content = new StringContent(serializer.Serialize(msg), Encoding.UTF8, "application/json");
                await httpClient.PostAsync($"{baseUrl}/sendFile?token={token}", content).ConfigureAwait(false);
            }
            else
            {
                var msg = new 
                { 
                    phone = to.TrimStart('+'), 
                    body 
                };

                using var content = new StringContent(serializer.Serialize(msg), Encoding.UTF8, "application/json");
                await httpClient.PostAsync($"{baseUrl}/sendMessage?token={token}", content).ConfigureAwait(false);
            }
        }
    }
}

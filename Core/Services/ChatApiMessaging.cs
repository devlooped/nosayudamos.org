using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NosAyudamos
{
    [NoExport]
    class ChatApiMessaging : IMessaging
    {
        readonly HttpClient http;
        readonly ISerializer serializer;
        readonly IEnvironment env;

        public ChatApiMessaging(IEnvironment env, HttpClient http, ISerializer serializer)
        {
            this.http = http;
            this.serializer = serializer;
            this.env = env;
        }

        public async Task SendTextAsync(string to, string body)
        {
            var baseUrl = env.GetVariable("ChatApiBaseUrl").TrimEnd('/');
            var token = env.GetVariable("ChatApiToken");

            // Markdown-like image format to allow for text + url
            if (body.StartsWith("![", StringComparison.Ordinal))
            {
                var text = body[2..body.LastIndexOf(']')];
                var url = body.Substring(body.LastIndexOf('(')).TrimStart('(').TrimEnd(')');

                // Post as file.
                var bytes = await http.GetByteArrayAsync(url);
                var msg = new
                {
                    phone = to.TrimStart('+'),
                    body = "data:image/gif;base64," + Convert.ToBase64String(bytes),
                    filename = "captcha.gif",
                    caption = text,
                };

                using var content = new StringContent(serializer.Serialize(msg), Encoding.UTF8, "application/json");
                await http.PostAsync($"{baseUrl}/sendFile?token={token}", content).ConfigureAwait(false);
            }
            else
            {
                var msg = new
                {
                    phone = to.TrimStart('+'),
                    body
                };

                using var content = new StringContent(serializer.Serialize(msg), Encoding.UTF8, "application/json");
                await http.PostAsync($"{baseUrl}/sendMessage?token={token}", content).ConfigureAwait(false);
            }
        }
    }
}

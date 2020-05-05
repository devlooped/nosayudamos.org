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
        readonly Lazy<string> apiUrl;

        public ChatApiMessaging(IEnvironment enviroment, HttpClient httpClient, ISerializer serializer)
        {
            this.httpClient = httpClient;
            this.serializer = serializer;
            apiUrl = new Lazy<string>(() => enviroment.GetVariable("ChatApiUrl"));
        }

        public async Task SendTextAsync(string from, string body, string to)
        {
            using var content = new StringContent(serializer.Serialize(new { phone = to.TrimStart('+'), body }), Encoding.UTF8, "application/json");
            await httpClient.PostAsync(apiUrl.Value, content).ConfigureAwait(false);
        }
    }
}

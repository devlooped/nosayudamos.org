using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace NosAyudamos
{
    [NoExport]
    class ChatApiMessaging : IMessaging
    {
        readonly MediaTypeFormatter formatter = new JsonMediaTypeFormatter();
        readonly HttpClient httpClient;
        Lazy<string> apiUrl;

        public ChatApiMessaging(IEnvironment enviroment, HttpClient httpClient)
        {
            this.httpClient = httpClient;
            apiUrl = new Lazy<string>(() => enviroment.GetVariable("ChatApiUrl"));
        }

        public async Task SendTextAsync(string from, string body, string to)
            => await httpClient.PostAsync(apiUrl.Value, new { phone = to.TrimStart('+'), body }, formatter).ConfigureAwait(false);
    }
}

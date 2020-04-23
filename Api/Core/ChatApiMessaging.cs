using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class ChatApiMessaging : IMessaging
    {
        readonly MediaTypeFormatter formatter = new JsonMediaTypeFormatter();
        readonly HttpClient httpClient;
        string apiUrl;

        public ChatApiMessaging(IEnvironment enviroment, HttpClient httpClient)
        {
            this.httpClient = httpClient;
            apiUrl = enviroment.GetVariable("ChatApiUrl");
        }

        public async Task SendTextAsync(string from, string body, string to)
        {
            await httpClient.PostAsync(apiUrl, new { phone = to.TrimStart('+'), body }, formatter).ConfigureAwait(false);
        }
    }
}

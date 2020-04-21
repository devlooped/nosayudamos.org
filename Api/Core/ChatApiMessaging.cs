using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class ChatApiMessaging : IMessaging
    {
        readonly MediaTypeFormatter formatter = new JsonMediaTypeFormatter();
        string apiUrl;

        public ChatApiMessaging(IEnvironment enviroment) => apiUrl = enviroment.GetVariable("ChatApiUrl");

        public async Task SendTextAsync(string from, string body, string to)
        {
            using var http = new HttpClient();
            await http.PostAsync(apiUrl, new { phone = to.TrimStart('+'), body }, formatter).ConfigureAwait(false);
        }
    }
}

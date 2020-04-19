using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace NosAyudamos
{
    public interface IMessaging
    {
        Task SendTextAsync(string from, string body, string to);
    }

    class Messaging : IMessaging, IDisposable
    {
        Lazy<IMessaging> twilio;
        Lazy<IMessaging> chatApi;
        string chatApiNumber;

        public Messaging(IEnviroment enviroment)
        {
            chatApiNumber = enviroment.GetVariable("ChatApiNumber");
            twilio = new Lazy<IMessaging>(() => new TwilioMessaging(enviroment));
            chatApi = new Lazy<IMessaging>(() => new ChatApiMessaging(enviroment));
        }

        public void Dispose()
        {
            if (chatApi.IsValueCreated && chatApi.Value is IDisposable cd)
                cd.Dispose();

            if (twilio.IsValueCreated && twilio.Value is IDisposable td)
                td.Dispose();
        }

        public Task SendTextAsync(string from, string body, string to)
        {
            if (from == chatApiNumber)
                return chatApi.Value.SendTextAsync(from, body, to);
            else
                return twilio.Value.SendTextAsync(from, body, to);
        }
    }

    class ChatApiMessaging : IMessaging
    {
        readonly MediaTypeFormatter formatter = new JsonMediaTypeFormatter();
        string apiUrl;

        public ChatApiMessaging(IEnviroment enviroment)
        {
            apiUrl = enviroment.GetVariable("ChatApiUrl");
        }

        public async Task SendTextAsync(string from, string body, string to)
        {
            using var http = new HttpClient();
            await http.PostAsync(apiUrl, new { phone = to.TrimStart('+'), body }, formatter).ConfigureAwait(false);
        }
    }

    class TwilioMessaging : IMessaging
    {
        public TwilioMessaging(IEnviroment enviroment)
        {
            TwilioClient.Init(
                enviroment.GetVariable("TwilioAccountSid"),
                enviroment.GetVariable("TwilioAuthToken"));
        }

        public async Task SendTextAsync(string from, string body, string to)
        {
            var message = await MessageResource.CreateAsync(
               from: new Twilio.Types.PhoneNumber(from),
               to: new Twilio.Types.PhoneNumber(to),
               body: body).ConfigureAwait(false);

            //return message.Sid;
        }
    }
}

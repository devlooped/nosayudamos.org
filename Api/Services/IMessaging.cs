using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace NosAyudamos
{
    interface IMessaging
    {
        Task SendTextAsync(string from, string body, string to);
    }

    class Messaging : IMessaging, IDisposable
    {
        readonly Lazy<IMessaging> twilio;
        readonly Lazy<IMessaging> chatApi;
        readonly Lazy<IMessaging> log;
        readonly IEnviroment enviroment;

        public Messaging(IEnviroment enviroment, ILogger<Messaging> logger)
        {
            this.enviroment = enviroment;
            twilio = new Lazy<IMessaging>(() => new TwilioMessaging(enviroment));
            chatApi = new Lazy<IMessaging>(() => new ChatApiMessaging(enviroment));
            log = new Lazy<IMessaging>(() => new LogMessaging(logger));
        }

        public void Dispose()
        {
            if (chatApi.IsValueCreated && chatApi.Value is IDisposable cd)
                cd.Dispose();

            if (twilio.IsValueCreated && twilio.Value is IDisposable td)
                td.Dispose();
        }

        public async Task SendTextAsync(string from, string body, string to)
        {
            var sendMessage = enviroment.GetVariable("SendMessages", true);

            if (sendMessage)
            {
                if (from == enviroment.GetVariable("ChatApiNumber"))
                    await chatApi.Value.SendTextAsync(from, body, to);
                else
                    await twilio.Value.SendTextAsync(from, body, to);
            }

            if (enviroment.GetVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
            {
                await log.Value.SendTextAsync(from, body, to);
            }
        }
    }

    class ChatApiMessaging : IMessaging
    {
        readonly MediaTypeFormatter formatter = new JsonMediaTypeFormatter();
        string apiUrl;

        public ChatApiMessaging(IEnviroment enviroment) => apiUrl = enviroment.GetVariable("ChatApiUrl");

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
               from: new Twilio.Types.PhoneNumber("+" + from),
               to: new Twilio.Types.PhoneNumber("+" + to),
               body: body).ConfigureAwait(false);

            //return message.Sid;
        }
    }

    class LogMessaging : IMessaging
    {
        readonly ILogger<IMessaging> logger;
        public LogMessaging(ILogger<IMessaging> logger) => this.logger = logger;

        public Task SendTextAsync(string from, string body, string to)
        {
            logger.LogInformation($@"From:{from}|To:|{to}
Body:{body}");

            return Task.CompletedTask;
        }
    }
}

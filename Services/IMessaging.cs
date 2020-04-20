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
        readonly IEnvironment environment;

        public Messaging(IEnvironment environment, ILogger<Messaging> logger)
        {
            this.environment = environment;
            twilio = new Lazy<IMessaging>(() => new TwilioMessaging(environment));
            chatApi = new Lazy<IMessaging>(() => new ChatApiMessaging(environment));
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
            var sendMessage = environment.GetVariable("SendMessages", true);

            if (sendMessage)
            {
                if (from == environment.GetVariable("ChatApiNumber"))
                    await chatApi.Value.SendTextAsync(from, body, to);
                else
                    await twilio.Value.SendTextAsync(from, body, to);
            }

            if (environment.GetVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
            {
                await log.Value.SendTextAsync(from, body, to);
            }
        }
    }

    class ChatApiMessaging : IMessaging
    {
        readonly MediaTypeFormatter formatter = new JsonMediaTypeFormatter();
        string apiUrl;

        public ChatApiMessaging(IEnvironment environment)
        {
            apiUrl = environment.GetVariable("ChatApiUrl");
        }

        public async Task SendTextAsync(string from, string body, string to)
        {
            using var http = new HttpClient();
            await http.PostAsync(apiUrl, new { phone = to.TrimStart('+'), body }, formatter).ConfigureAwait(false);
        }
    }

    class TwilioMessaging : IMessaging
    {
        public TwilioMessaging(IEnvironment environment)
        {
            TwilioClient.Init(
                environment.GetVariable("TwilioAccountSid"),
                environment.GetVariable("TwilioAuthToken"));
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

    class LogMessaging : IMessaging
    {
        readonly ILogger<IMessaging> logger;
        public LogMessaging(ILogger<IMessaging> logger) => this.logger = logger;

        public Task SendTextAsync(string from, string body, string to)
        {
            logger.LogInformation($"From:{from}|Body:{body}To:|{to}");

            return Task.CompletedTask;
        }
    }
}

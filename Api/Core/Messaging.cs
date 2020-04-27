using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NosAyudamos.Events;
using Polly.Registry;

namespace NosAyudamos
{
    class Messaging : IMessaging, IDisposable
    {
        readonly Lazy<string> chatApiNumber;

        readonly Lazy<IMessaging> twilio;
        readonly Lazy<IMessaging> chatApi;
        readonly Lazy<IMessaging> log;
        readonly IEnvironment enviroment;

        public Messaging(IReadOnlyPolicyRegistry<string> registry, IEnvironment enviroment, HttpClient httpClient, ILogger<Messaging> logger)
        {
            this.enviroment = enviroment;
            twilio = new Lazy<IMessaging>(() => new TwilioMessaging(registry, enviroment));
            chatApi = new Lazy<IMessaging>(() => new ChatApiMessaging(enviroment, httpClient));
            log = new Lazy<IMessaging>(() => new LogMessaging(logger));

            chatApiNumber = new Lazy<string>(() => enviroment.GetVariable("ChatApiNumber").TrimStart('+'));
        }

        public void Dispose()
        {
            if (chatApi.IsValueCreated && chatApi.Value is IDisposable cd)
                cd.Dispose();

            if (twilio.IsValueCreated && twilio.Value is IDisposable td)
                td.Dispose();
        }

        public Task HandleAsync(MessageSent e) => SendTextAsync(e.From, e.Body, e.To);

        public async Task SendTextAsync(string from, string body, string to)
        {
            var sendMessage = enviroment.GetVariable("SendMessages", true);

            if (sendMessage)
            {
                if (from == chatApiNumber.Value)
                    await chatApi.Value.SendTextAsync(from, body, to);
                else
                    await twilio.Value.SendTextAsync(from, body, to);
            }

            if (enviroment.IsDevelopment())
            {
                await log.Value.SendTextAsync(from, body, to);
            }
        }
    }

    interface IMessaging
    {
        Task SendTextAsync(string from, string body, string to);
    }
}

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NosAyudamos.Events;

namespace NosAyudamos
{
    class Messaging : IMessaging, IDisposable
    {
        readonly Lazy<string> chatApiNumber;

        readonly Lazy<IMessaging> twilio;
        readonly Lazy<IMessaging> chatApi;
        readonly Lazy<IMessaging> log;
        readonly IEnvironment environment;

        public Messaging(IReadOnlyPolicyRegistry<string> registry, IEnvironment environment, HttpClient httpClient, ILogger<Messaging> logger)
        {
            this.environment = environment;
            twilio = new Lazy<IMessaging>(() => new TwilioMessaging(registry, environment));
            chatApi = new Lazy<IMessaging>(() => new ChatApiMessaging(environment, httpClient));
            log = new Lazy<IMessaging>(() => new LogMessaging(logger));

            chatApiNumber = new Lazy<string>(() => environment.GetVariable("ChatApiNumber").TrimStart('+'));
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
            var sendMessage = !environment.IsDevelopment() || environment.GetVariable("SendToMessagingInDevelopment", false);

            if (sendMessage)
            {
                if (from == chatApiNumber.Value)
                    await chatApi.Value.SendTextAsync(from, body, to);
                else
                    await twilio.Value.SendTextAsync(from, body, to);
            }

            if (environment.IsDevelopment())
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

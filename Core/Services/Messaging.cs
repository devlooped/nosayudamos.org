using System;
using System.Composition;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly.Registry;

namespace NosAyudamos
{
    [Shared]
    class Messaging : IMessaging, IDisposable
    {
        readonly Lazy<string> chatApiNumber;

        readonly Lazy<IMessaging> twilio;
        readonly Lazy<IMessaging> chatApi;
        readonly Lazy<IMessaging> log;
        readonly IEnvironment environment;
        readonly IEntityRepository<PhoneSystem> phoneRepo;

        public Messaging(
            IReadOnlyPolicyRegistry<string> registry, IEnvironment environment,
            IEntityRepository<PhoneSystem> phoneRepo, HttpClient httpClient,
            ISerializer serializer, ILogger<Messaging> logger)
        {
            this.environment = environment;
            this.phoneRepo = phoneRepo;
            twilio = new Lazy<IMessaging>(() => new TwilioMessaging(registry, environment));
            chatApi = new Lazy<IMessaging>(() => new ChatApiMessaging(environment, httpClient, serializer));
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

        public Task HandleAsync(MessageSent e) => SendTextAsync(e.PhoneNumber, e.Body);

        public async Task SendTextAsync(string to, string body)
        {
            var sendMessage = !environment.IsDevelopment() || environment.GetVariable("SendToMessagingInDevelopment", false);

            if (sendMessage)
            {
                var map = await phoneRepo.GetAsync(to);
                if (map == null)
                {
                    // TODO: log error
                    return;
                }

                if (map.SystemNumber == chatApiNumber.Value)
                    await chatApi.Value.SendTextAsync(to, body);
                else
                    await twilio.Value.SendTextAsync(to, body);
            }

            if (environment.IsDevelopment())
            {
                await log.Value.SendTextAsync(to, body);
            }
        }
    }

    interface IMessaging
    {
        Task SendTextAsync(string to, string body);
    }
}

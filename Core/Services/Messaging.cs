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
        readonly IEnvironment env;
        readonly IEntityRepository<PhoneEntry> phoneDir;

        public Messaging(
            IReadOnlyPolicyRegistry<string> registry, IEnvironment env,
            IEntityRepository<PhoneEntry> phoneDir, HttpClient http,
            ISerializer serializer, ILogger<Messaging> logger)
        {
            this.env = env;
            this.phoneDir = phoneDir;
            twilio = new Lazy<IMessaging>(() => new TwilioMessaging(registry, env));
            chatApi = new Lazy<IMessaging>(() => new ChatApiMessaging(env, http, serializer));
            log = new Lazy<IMessaging>(() => new LogMessaging(logger));

            chatApiNumber = new Lazy<string>(() => env.GetVariable("ChatApiNumber").TrimStart('+'));
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
            var sendMessage = !env.IsDevelopment() || env.GetVariable("SendToMessagingInDevelopment", false);

            if (sendMessage)
            {
                var map = await phoneDir.GetAsync(to);
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

            if (env.IsDevelopment())
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

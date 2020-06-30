using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace NosAyudamos
{
    [NoExport]
    class TwilioMessaging : IMessaging
    {
        static bool initialized;
        readonly IEnvironment env;
        readonly IReadOnlyPolicyRegistry<string> registry;

        public TwilioMessaging(IReadOnlyPolicyRegistry<string> registry, IEnvironment env)
            => (this.registry, this.env)
            = (registry, env);

        public async Task SendTextAsync(string to, string body)
        {
            if (!initialized)
            {
                TwilioClient.Init(
                    env.GetVariable("TwilioAccountSid"),
                    env.GetVariable("TwilioAuthToken"));

                initialized = true;
            }

            var policy = registry.Get<IAsyncPolicy>("TwilioPolicy");

            await policy.ExecuteAsync(async () =>
                await MessageResource.CreateAsync(
                   from: new Twilio.Types.PhoneNumber("whatsapp:" + env.GetVariable("TwilioNumber")),
                   to: new Twilio.Types.PhoneNumber("whatsapp:+" + to),
                   body: body).ConfigureAwait(false));
        }
    }
}

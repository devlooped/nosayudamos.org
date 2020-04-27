using System.Composition;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace NosAyudamos
{
    [Shared]
    class TwilioMessaging : IMessaging
    {
        static bool initialized;
        readonly IEnvironment environment;
        readonly IReadOnlyPolicyRegistry<string> registry;

        public TwilioMessaging(IReadOnlyPolicyRegistry<string> registry, IEnvironment environment) => (this.registry, this.environment) = (registry, environment);


        public async Task SendTextAsync(string from, string body, string to)
        {
            if (!initialized)
            {
                TwilioClient.Init(
                    environment.GetVariable("TwilioAccountSid"),
                    environment.GetVariable("TwilioAuthToken"));

                initialized = true;
            }


            var policy = registry.Get<IAsyncPolicy>("TwilioPolicy");

            await policy.ExecuteAsync(async () =>
                await MessageResource.CreateAsync(
                   from: new Twilio.Types.PhoneNumber("whatsapp:+" + from),
                   to: new Twilio.Types.PhoneNumber("whatsapp:+" + to),
                   body: body).ConfigureAwait(false));
        }
    }
}

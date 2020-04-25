using System.Composition;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace NosAyudamos
{
    [Shared]
    class TwilioMessaging : IMessaging
    {
        static bool initialized;
        IEnvironment environment;

        public TwilioMessaging(IEnvironment environment) => this.environment = environment;

        public async Task SendTextAsync(string from, string body, string to)
        {
            if (!initialized)
            {
                TwilioClient.Init(
                    environment.GetVariable("TwilioAccountSid"),
                    environment.GetVariable("TwilioAuthToken"));

                initialized = true;
            }

            var message = await MessageResource.CreateAsync(
               from: new Twilio.Types.PhoneNumber("whatsapp:+" + from),
               to: new Twilio.Types.PhoneNumber("whatsapp:+" + to),
               body: body).ConfigureAwait(false);
        }
    }
}

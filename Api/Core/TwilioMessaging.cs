using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace NosAyudamos
{
    class TwilioMessaging : IMessaging
    {
        public TwilioMessaging(IEnvironment enviroment)
        {
            TwilioClient.Init(
                enviroment.GetVariable("TwilioAccountSid"),
                enviroment.GetVariable("TwilioAuthToken"));
        }

        public async Task SendTextAsync(string from, string body, string to)
        {
            var message = await MessageResource.CreateAsync(
               from: new Twilio.Types.PhoneNumber("whatsapp:+" + from),
               to: new Twilio.Types.PhoneNumber("whatsapp:+" + to),
               body: body).ConfigureAwait(false);
        }
    }
}

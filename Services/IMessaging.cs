using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace NosAyudamos
{
    public interface IMessaging
    {
        Task<string> SendTextAsync(string from, string body, string to);
    }

    public class Messaging : IMessaging
    {
        public Messaging(IEnviroment enviroment)
        {
            Contract.Assert(enviroment != null);

            TwilioClient.Init(
                enviroment.GetVariable("TwilioAccountSid"),
                enviroment.GetVariable("TwilioAuthToken"));
        }

        public async Task<string> SendTextAsync(string from, string body, string to)
        {
            var message = await MessageResource.CreateAsync(
               from: new Twilio.Types.PhoneNumber("whatsapp:+" + from?.TrimStart('+')),
               to: new Twilio.Types.PhoneNumber("whatsapp:+" + to?.TrimStart('+')),
               body: body).ConfigureAwait(false);

            return message.Sid;
        }
    }
}

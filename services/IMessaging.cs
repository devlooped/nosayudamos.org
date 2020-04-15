using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace NosAyudamos
{
    public interface IMessaging
    {
        Task<string> SendText(string from, string body, string to);
    }

    public class Messaging : IMessaging
    {
        private readonly IEnviroment enviroment;
        public Messaging(IEnviroment env)
        {
            enviroment = env;

            TwilioClient.Init(
                enviroment.GetVariable("TwilioAccountSid"),
                enviroment.GetVariable("TwilioAuthToken"));
        }

        public async Task<string> SendText(string from, string body, string to)
        {
             var message = await MessageResource.CreateAsync(
                new Twilio.Types.PhoneNumber(from),
                body,
                new Twilio.Types.PhoneNumber(to)).ConfigureAwait(false);

            return message.Sid;
        }
    }
}
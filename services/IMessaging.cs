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
        private readonly IEnviroment _enviroment;
        public Messaging(IEnviroment enviroment)
        {
            _enviroment = enviroment;

            TwilioClient.Init(
                _enviroment.GetVariable("TwilioAccountSid"),
                _enviroment.GetVariable("TwilioAuthToken"));
        }

        public async Task<string> SendText(string from, string body, string to)
        {
            Exceptions.ThrowIfNullOrEmpty(from, nameof(from));
            Exceptions.ThrowIfNullOrEmpty(body, nameof(body));
            Exceptions.ThrowIfNullOrEmpty(to, nameof(to));

             var message = await MessageResource.CreateAsync(
                new Twilio.Types.PhoneNumber(from),
                body,
                new Twilio.Types.PhoneNumber(to)).ConfigureAwait(false);

            return message.Sid;
        }
    }
}
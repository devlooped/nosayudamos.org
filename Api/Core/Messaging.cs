using System;
using System.Threading.Tasks;
using Serilog;

namespace NosAyudamos
{
    class Messaging : IMessaging, IDisposable
    {
        readonly Lazy<IMessaging> twilio;
        readonly Lazy<IMessaging> chatApi;
        readonly Lazy<IMessaging> log;
        readonly IEnvironment enviroment;

        public Messaging(IEnvironment enviroment, ILogger logger)
        {
            this.enviroment = enviroment;
            twilio = new Lazy<IMessaging>(() => new TwilioMessaging(enviroment));
            chatApi = new Lazy<IMessaging>(() => new ChatApiMessaging(enviroment));
            log = new Lazy<IMessaging>(() => new LogMessaging(logger));
        }

        public void Dispose()
        {
            if (chatApi.IsValueCreated && chatApi.Value is IDisposable cd)
                cd.Dispose();

            if (twilio.IsValueCreated && twilio.Value is IDisposable td)
                td.Dispose();
        }

        public async Task SendTextAsync(string from, string body, string to)
        {
            var sendMessage = enviroment.GetVariable("SendMessages", true);

            if (sendMessage)
            {
                if (from == enviroment.GetVariable("ChatApiNumber"))
                    await chatApi.Value.SendTextAsync(from, body, to);
                else
                    await twilio.Value.SendTextAsync(from, body, to);
            }

            if (enviroment.GetVariable("AZURE_FUNCTIONS_ENVIRONMENT", "Production") == "Development")
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

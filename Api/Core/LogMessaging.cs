using System.Composition;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    [Shared]
    class LogMessaging : IMessaging
    {
        readonly ILogger<IMessaging> logger;

        public LogMessaging(ILogger<IMessaging> logger) => this.logger = logger;

        public Task SendTextAsync(string from, string body, string to)
        {
            logger.LogInformation($@"From:{from}|To:|{to}
Body:{body}");

            return Task.CompletedTask;
        }
    }
}

using System.Threading.Tasks;
using Serilog;

namespace NosAyudamos
{
    class LogMessaging : IMessaging
    {
        readonly ILogger logger;

        public LogMessaging(ILogger logger) => this.logger = logger;

        public Task SendTextAsync(string from, string body, string to)
        {
            logger.Information($@"From:{from}|To:|{to}
Body:{body}");

            return Task.CompletedTask;
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using Merq;
using Microsoft.Azure.WebJobs;

namespace NosAyudamos.EventGrid
{
    class TaxValidation
    {
        readonly ICommandBus commands;

        public TaxValidation(ICommandBus commands) => this.commands = commands;

        [FunctionName("tax-validate")]
        public Task ExecuteAsync([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timer)
            => commands.ExecuteAsync(new ValidateTaxStatusBatch(), CancellationToken.None);
    }
}

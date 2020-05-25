using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Can't remove it because Azure Functions requires it.")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Can't remove it because Azure Functions requires it.")]
        public Task ExecuteAsync([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timer)
            => commands.ExecuteAsync(new ValidateTaxStatusBatch(), CancellationToken.None);
    }
}

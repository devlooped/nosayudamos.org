using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace NosAyudamos.EventGrid
{
    class TaxValidation
    {
        readonly IEntityRepository<TaxStatusValidation> repository;
        readonly TaxStatusValidationHandler handler;

        public TaxValidation(IEntityRepository<TaxStatusValidation> repository, TaxStatusValidationHandler handler)
            => (this.handler, this.repository)
            = (handler, repository);

        [FunctionName("tax-validate")]
        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Can't remove it because Azure Functions requires it.")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Can't remove it because Azure Functions requires it.")]
        public async Task ExecuteAsync([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timer)
        {
            await foreach (var validation in repository.GetAllAsync())
            {
                await handler.ExecuteAsync(validation);
            }
        }
    }
}

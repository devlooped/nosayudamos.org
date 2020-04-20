using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    [Workflow("Donor")]
    class DonorWorkflow : IWorkflow
    {
        readonly IEnvironment environment;
        readonly ITextAnalysis textAnalysis;
        readonly ILogger<DonorWorkflow> logger;
        readonly IMessaging messaging;
        readonly IRepositoryFactory repositoryFactory;

        public DonorWorkflow(IEnvironment environment,
                            ITextAnalysis textAnalysis,
                            IMessaging messaging,
                            IRepositoryFactory repositoryFactory,
                            ILogger<DonorWorkflow> logger) =>
                            (this.environment, this.textAnalysis, this.messaging, this.repositoryFactory, this.logger) =
                                (environment, textAnalysis, messaging, repositoryFactory, logger);
        public async Task RunAsync(Message message)
        {
            //TODO: implement state machine
            await messaging.SendTextAsync(
                message.To, "Gracias por contactarnos, cuanto dinero desea donar?", message.From);
        }
    }
}

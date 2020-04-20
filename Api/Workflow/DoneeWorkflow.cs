using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    [Workflow("Donee")]
    class DoneeWorkflow : IWorkflow
    {
        readonly IEnviroment enviroment;
        readonly ITextAnalysis textAnalysis;
        readonly ILogger<DoneeWorkflow> logger;
        readonly IMessaging messaging;
        readonly IRepositoryFactory repositoryFactory;

        public DoneeWorkflow(IEnviroment enviroment,
                            ITextAnalysis textAnalysis,
                            IMessaging messaging,
                            IRepositoryFactory repositoryFactory,
                            ILogger<DoneeWorkflow> logger) =>
                            (this.enviroment, this.textAnalysis, this.messaging, this.repositoryFactory, this.logger) =
                                (enviroment, textAnalysis, messaging, repositoryFactory, logger);
        public async Task RunAsync(Message message)
        {
            //TODO: implement state machine
            await messaging.SendTextAsync(
                message.To, "Cuanto dinero necesita?", message.From);
        }
    }
}

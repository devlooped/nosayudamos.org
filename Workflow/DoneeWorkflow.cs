using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    [Workflow("Donee")]
    class DoneeWorkflow : IWorkflow
    {
        readonly IEnvironment environment;
        readonly ITextAnalysis textAnalysis;
        readonly ILogger<DoneeWorkflow> logger;
        readonly IMessaging messaging;
        readonly IRepositoryFactory repositoryFactory;

        public DoneeWorkflow(IEnvironment environment,
                            ITextAnalysis textAnalysis,
                            IMessaging messaging,
                            IRepositoryFactory repositoryFactory,
                            ILogger<DoneeWorkflow> logger) =>
                            (this.environment, this.textAnalysis, this.messaging, this.repositoryFactory, this.logger) =
                                (environment, textAnalysis, messaging, repositoryFactory, logger);
        public async Task RunAsync(Message message)
        {
            //TODO: implement state machine
            await messaging.SendTextAsync(
                message.To, "Cuanto dinero necesita?", message.From);
        }
    }
}

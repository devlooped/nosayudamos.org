using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    [Workflow("Donor")]
    class DonorWorkflow : IWorkflow
    {
        readonly IEnvironment enviroment;
        readonly ITextAnalysis textAnalysis;
        readonly ILogger<DonorWorkflow> logger;
        readonly IMessaging messaging;
        readonly IRepositoryFactory repositoryFactory;

        public DonorWorkflow(IEnvironment enviroment,
                            ITextAnalysis textAnalysis,
                            IMessaging messaging,
                            IRepositoryFactory repositoryFactory,
                            ILogger<DonorWorkflow> logger) =>
                            (this.enviroment, this.textAnalysis, this.messaging, this.repositoryFactory, this.logger) =
                                (enviroment, textAnalysis, messaging, repositoryFactory, logger);

        public Task RunAsync(MessageEvent message) => Task.CompletedTask;
    }
}

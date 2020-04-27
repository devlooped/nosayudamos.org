using System.Composition;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NosAyudamos.Events;

namespace NosAyudamos
{
    [Export]
    [Workflow("Donee")]
    class DoneeWorkflow : IWorkflow
    {
        readonly IEnvironment enviroment;
        readonly ITextAnalysis textAnalysis;
        readonly ILogger<DoneeWorkflow> logger;
        readonly IMessaging messaging;
        readonly IRepositoryFactory repositoryFactory;

        public DoneeWorkflow(IEnvironment enviroment,
                            ITextAnalysis textAnalysis,
                            IMessaging messaging,
                            IRepositoryFactory repositoryFactory,
                            ILogger<DoneeWorkflow> logger) =>
                            (this.enviroment, this.textAnalysis, this.messaging, this.repositoryFactory, this.logger) =
                                (enviroment, textAnalysis, messaging, repositoryFactory, logger);

        public Task RunAsync(MessageEvent message) => Task.CompletedTask;
    }
}

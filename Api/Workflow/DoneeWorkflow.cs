using System.Threading.Tasks;
using Serilog;

namespace NosAyudamos
{
    class DoneeWorkflow : IWorkflow
    {
        readonly IEnvironment enviroment;
        readonly ITextAnalysis textAnalysis;
        readonly ILogger logger;
        readonly IMessaging messaging;
        readonly IRepositoryFactory repositoryFactory;

        public DoneeWorkflow(IEnvironment enviroment,
                            ITextAnalysis textAnalysis,
                            IMessaging messaging,
                            IRepositoryFactory repositoryFactory,
                            ILogger logger) =>
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

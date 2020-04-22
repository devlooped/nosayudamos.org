using System.Threading.Tasks;
using Serilog;

namespace NosAyudamos
{
    class DonorWorkflow : IWorkflow
    {
        readonly IEnvironment enviroment;
        readonly ITextAnalysis textAnalysis;
        readonly ILogger logger;
        readonly IMessaging messaging;
        readonly IRepositoryFactory repositoryFactory;

        public DonorWorkflow(IEnvironment enviroment,
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
                message.To, "Gracias por contactarnos, cuanto dinero desea donar?", message.From);
        }
    }
}

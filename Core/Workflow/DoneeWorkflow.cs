using System.Threading.Tasks;
using Merq;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    [Workflow(nameof(Role.Donee))]
    class DoneeWorkflow : IWorkflow
    {
        readonly IEnvironment enviroment;
        readonly ICommandBus commands;
        readonly ILanguageUnderstanding language;
        readonly ILogger<DoneeWorkflow> logger;
        readonly ITaxIdRecognizer recognizer;
        readonly IBlobStorage storage;

        public DoneeWorkflow(
            IEnvironment enviroment, ICommandBus commands,
            ILanguageUnderstanding language,
            ITaxIdRecognizer recognizer, IBlobStorage blobStorage, ILogger<DoneeWorkflow> logger)
            => (this.enviroment, this.commands, this.language, this.recognizer, this.storage, this.logger)
            = (enviroment, commands, language, recognizer, blobStorage, logger);

        public Task RunAsync(MessageEvent @event, Person? person)
        {
            if (person == null || !(@event is MessageReceived message))
                return Task.CompletedTask;

            return Task.CompletedTask;
        }
    }
}

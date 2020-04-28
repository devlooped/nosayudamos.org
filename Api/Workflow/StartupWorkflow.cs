using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NosAyudamos.Events;

namespace NosAyudamos
{
    [SuppressMessage("Microsoft.Design", "CA1040")]
    interface IStartupWorkflow : IWorkflow
    {
    }

    [Workflow("Startup")]
    class StartupWorkflow : IWorkflow, IStartupWorkflow
    {
        readonly IEnvironment enviroment;
        readonly ILanguageUnderstanding languageUnderstanding;
        readonly Lazy<IWorkflowSelector> workflowSelector;
        readonly IBlobStorage blobStorage;
        readonly ILogger<StartupWorkflow> logger;
        readonly IMessaging messaging;
        readonly IPersonRecognizer personRecognizer;
        readonly IRepositoryFactory repositoryFactory;
        readonly PersonRepository personRepository;
        readonly HttpClient httpClient;
        readonly InteractiveAction interactiveAction;

        public StartupWorkflow(IEnvironment enviroment,
                            ILanguageUnderstanding languageUnderstanding,
                            IMessaging messaging,
                            Lazy<IWorkflowSelector> workflowSelector,
                            IBlobStorage blobStorage,
                            IPersonRecognizer personRecognizer,
                            IRepositoryFactory repositoryFactory,
                            PersonRepository personRepository,
                            HttpClient httpClient,
                            InteractiveAction interactiveAction,
                            ILogger<StartupWorkflow> logger) =>
                            (this.enviroment, this.languageUnderstanding, this.messaging, this.workflowSelector, this.blobStorage, this.personRecognizer, this.repositoryFactory, this.personRepository, this.httpClient, this.interactiveAction, this.logger) =
                                (enviroment, languageUnderstanding, messaging, workflowSelector, blobStorage, personRecognizer, repositoryFactory, personRepository, httpClient, interactiveAction, logger);

        public async Task RunAsync(MessageEvent @event) => await RegisterDoneeAsync((ImageMessageReceived)@event);

        private async Task RegisterDoneeAsync(ImageMessageReceived @event)
        {
#pragma warning disable CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
            var person = await interactiveAction.ExecuteAsync(
#pragma warning restore CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
                () => personRecognizer.RecognizeAsync(@event.ImageUri),
                () => messaging.SendTextAsync(@event.To, "Dni invalido, intente de nuevo.", @event.From),
                () => messaging.SendTextAsync(@event.To, "No pudimos procesar su dni. Nos contactaremos en breve.", @event.From));

            if (person != null)
            {
                var image = await httpClient.GetByteArrayAsync(@event.ImageUri);

                await blobStorage.UploadAsync(
                    image, enviroment.GetVariable("AttachmentsContainerName"), $"dni_{person.NationalId}.png");

                person.PhoneNumber = @event.From;
            }
        }
    }
}

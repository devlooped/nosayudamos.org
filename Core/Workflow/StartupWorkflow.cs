using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
        readonly IPersonalIdRecognizer idRecognizer;
        readonly IRepositoryFactory repositoryFactory;
        readonly PersonRepository personRepository;
        readonly HttpClient httpClient;
        readonly InteractiveAction interactiveAction;

        public StartupWorkflow(IEnvironment enviroment,
                            ILanguageUnderstanding languageUnderstanding,
                            IMessaging messaging,
                            Lazy<IWorkflowSelector> workflowSelector,
                            IBlobStorage blobStorage,
                            IPersonalIdRecognizer idRecognizer,
                            IRepositoryFactory repositoryFactory,
                            PersonRepository personRepository,
                            HttpClient httpClient,
                            InteractiveAction interactiveAction,
                            ILogger<StartupWorkflow> logger)
            => (this.enviroment, this.languageUnderstanding, this.messaging, this.workflowSelector, this.blobStorage, this.idRecognizer, this.repositoryFactory, this.personRepository, this.httpClient, this.interactiveAction, this.logger)
            = (enviroment, languageUnderstanding, messaging, workflowSelector, blobStorage, idRecognizer, repositoryFactory, personRepository, httpClient, interactiveAction, logger);

        public async Task RunAsync(MessageEvent @event) => await RegisterDoneeAsync((MessageReceived)@event);

        private async Task RegisterDoneeAsync(MessageReceived e)
        {
#pragma warning disable CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
            var id = await interactiveAction.ExecuteAsync(
#pragma warning restore CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
                () => idRecognizer.RecognizeAsync(e.Body),
                () => messaging.SendTextAsync(e.To, "Dni invalido, intente de nuevo.", e.From),
                () => messaging.SendTextAsync(e.To, "No pudimos procesar su dni. Nos contactaremos en breve.", e.From));

            if (id != null)
            {
                var image = await httpClient.GetByteArrayAsync(e.Body);

                await blobStorage.UploadAsync(
                    image, enviroment.GetVariable("AttachmentsContainerName"), $"dni_{id.NationalId}.png");

                var person = await personRepository.GetAsync(id.NationalId, readOnly: false);
                if (person == null)
                {
                    await personRepository.PutAsync(
                        new Person(id.FirstName, id.LastName, id.NationalId, e.From, id.DateOfBirth, id.Sex));
                }
                else
                {
                    person.UpdatePhoneNumber(e.From);
                    await personRepository.PutAsync(person);
                }
            }
        }
    }
}
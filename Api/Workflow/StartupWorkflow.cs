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

        public StartupWorkflow(IEnvironment enviroment,
                            ILanguageUnderstanding languageUnderstanding,
                            IMessaging messaging,
                            Lazy<IWorkflowSelector> workflowSelector,
                            IBlobStorage blobStorage,
                            IPersonRecognizer personRecognizer,
                            IRepositoryFactory repositoryFactory,
                            PersonRepository personRepository,
                            HttpClient httpClient,
                            ILogger<StartupWorkflow> logger) =>
                            (this.enviroment, this.languageUnderstanding, this.messaging, this.workflowSelector, this.blobStorage, this.personRecognizer, this.repositoryFactory, this.personRepository, this.httpClient, this.logger) =
                                (enviroment, languageUnderstanding, messaging, workflowSelector, blobStorage, personRecognizer, repositoryFactory, personRepository, httpClient, logger);

        public async Task RunAsync(MessageEvent @event)
        {
            //TODO: Find person by phone number and execute person workflow
            var message = (TextMessageReceived)@event;

            if (Uri.TryCreate(message.Text, UriKind.Absolute, out var url))
            {
                await RegisterDoneeAsync(message);
            }
            else
            {
                var intents = await languageUnderstanding.GetIntentsAsync(message.Text);

                if (intents.ContainsKey("help"))
                {
                    await messaging.SendTextAsync(
                        message.To, "Gracias por contactarnos, envianos foto de tu DNI para registarte primero.", message.From);
                }
                else if (intents.ContainsKey("donate"))
                {
                    var workflow = workflowSelector.Value.Select(Workflow.Donor);
                    await workflow.RunAsync(message);
                }
                else
                {
                    await messaging.SendTextAsync(
                        message.To, "Gracias por contactarnos, querés ayudar o necesitás ayuda?", message.From);
                }
            }
        }

        private async Task RegisterDoneeAsync(TextMessageReceived message)
        {
            if (Uri.TryCreate(message.Text, UriKind.Absolute, out var imageUri))
            {
                var person = await personRecognizer.RecognizeAsync(imageUri);
                var actionRetryRepository = repositoryFactory.Create<ActionRetryEntity>();

                if (person != null)
                {
                    var image = await httpClient.GetByteArrayAsync(imageUri);

                    await blobStorage.UploadAsync(
                        image, enviroment.GetVariable("AttachmentsContainerName"), $"dni_{person.NationalId}.png");

                    person.PhoneNumber = message.From;

                    await personRepository.PutAsync(person);

                    var actionRetry = await actionRetryRepository.GetAsync(message.To, Action.RecognizeId.ToString());

                    if (actionRetry != null)
                    {
                        await actionRetryRepository.DeleteAsync(actionRetry);
                    }
                }
                else
                {
                    var actionRetry = await actionRetryRepository.GetAsync(message.To, Action.RecognizeId.ToString());

                    if (actionRetry == null)
                    {
                        await actionRetryRepository.PutAsync(new ActionRetryEntity(message.To, Action.RecognizeId.ToString()));
                    }
                    else
                    {
                        if (actionRetry.RetryCount < 3)
                        {
                            actionRetry.RetryCount += 1;
                            await actionRetryRepository.PutAsync(actionRetry);
                        }
                        else
                        {
                            await messaging.SendTextAsync(
                                message.To, "No pudimos procesar su dni. Nos contactaremos en breve.", message.From);

                            logger.LogWarning(@"Unable to process national_id.
Message: {@message:j}", message);

                            return;
                        }
                    }

                    await messaging.SendTextAsync(
                        message.To, "Dni invalido, intente de nuevo.", message.From);
                }
            }
        }
    }
}

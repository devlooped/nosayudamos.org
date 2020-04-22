using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace NosAyudamos
{
    class StartupWorkflow : IWorkflow, IStartupWorkflow
    {
        readonly IEnvironment enviroment;
        readonly ILanguageUnderstanding languageUnderstanding;
        readonly IWorkflowFactory workflowFactory;
        readonly IBlobStorage blobStorage;
        readonly ILogger logger;
        readonly IMessaging messaging;
        readonly IPersonRecognizer personRecognizer;
        readonly IRepositoryFactory repositoryFactory;

        public StartupWorkflow(IEnvironment enviroment,
                            ILanguageUnderstanding languageUnderstanding,
                            IMessaging messaging,
                            IWorkflowFactory workflowFactory,
                            IBlobStorage blobStorage,
                            IPersonRecognizer personRecognizer,
                            IRepositoryFactory repositoryFactory,
                            ILogger logger) =>
                            (this.enviroment, this.languageUnderstanding, this.messaging, this.workflowFactory, this.blobStorage, this.personRecognizer, this.repositoryFactory, this.logger) =
                                (enviroment, languageUnderstanding, messaging, workflowFactory, blobStorage, personRecognizer, repositoryFactory, logger);

        public async Task RunAsync(Message message)
        {
            //TODO: Find person by phone number and execute person workflow

            if (Uri.TryCreate(message.Body, UriKind.Absolute, out var url))
            {
                await RegisterDoneeAsync(message);
            }
            else
            {
                var intents = await languageUnderstanding.GetIntentsAsync(message.Body);

                if (intents.Contains("help"))
                {
                    await messaging.SendTextAsync(
                        message.To, "Gracias por contactarnos, envianos foto de tu DNI para registarte primero.", message.From);
                }
                else if (intents.Contains("donate"))
                {
                    var workflow = workflowFactory.Create(Workflow.Donor);
                    await workflow.RunAsync(message);
                }
                else
                {
                    await messaging.SendTextAsync(
                        message.To, "Gracias por contactarnos, querés ayudar o necesitás ayuda?", message.From);
                }
            }
        }

        private async Task RegisterDoneeAsync(Message message)
        {
            if (Uri.TryCreate(message.Body, UriKind.Absolute, out var imageUri))
            {
                var person = await personRecognizer.RecognizeAsync(imageUri);
                var actionRetryRepository = repositoryFactory.Create<ActionRetryEntity>();

                if (person != null)
                {
                    var image = await Utility.DownloadBlobAsync(imageUri);

                    await blobStorage.UploadAsync(
                        image, enviroment.GetVariable("AttachmentsContainerName"), $"dni_{person.NationalId}.png");

                    var doneeRepository = repositoryFactory.Create<DoneeEntity>();

                    await doneeRepository.PutAsync(
                        new DoneeEntity(person.NationalId,
                                        person.FirstName,
                                        person.LastName,
                                        person.DateOfBirth,
                                        person.Sex));

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

                            logger.Warning(@"Unable to process national_id.
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

    [SuppressMessage("Microsoft.Design", "CA1040")]
    interface IStartupWorkflow : IWorkflow
    {
    }
}

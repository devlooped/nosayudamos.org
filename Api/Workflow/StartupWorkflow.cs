using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        readonly IEnviroment enviroment;
        readonly ILanguageUnderstanding languageUnderstanding;
        readonly IWorkflowFactory workflowFactory;
        readonly IBlobStorage blobStorage;
        readonly ILogger<StartupWorkflow> logger;
        readonly IMessaging messaging;
        readonly IPersonRecognizer personRecognizer;
        readonly IRepositoryFactory repositoryFactory;

        public StartupWorkflow(IEnviroment enviroment,
                            ILanguageUnderstanding languageUnderstanding,
                            IMessaging messaging,
                            IWorkflowFactory workflowFactory,
                            IBlobStorage blobStorage,
                            IPersonRecognizer personRecognizer,
                            IRepositoryFactory repositoryFactory,
                            ILogger<StartupWorkflow> logger) =>
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
                        message.To, "Gracias por contactarnos, envie foto de dni para registarse primero.", message.From);
                }
                else if (intents.Contains("donate"))
                {
                    var workflow = workflowFactory.Create(Workflow.Donor);
                    await workflow.RunAsync(message);
                }
                else
                {
                    await messaging.SendTextAsync(
                        message.To, "Gracias por contactarnos, desea ayudar o necesita ayuda?", message.From);
                }
            }
        }

        private async Task RegisterDoneeAsync(Message message)
        {
            if (Uri.TryCreate(message.Body, UriKind.Absolute, out var imageUri))
            {
                var person = await this.personRecognizer.RecognizeAsync(imageUri);
                var actionRetryRepository = repositoryFactory.Create<ActionRetryEntity>();

                if (person != null)
                {
                    var image = await Utility.DownloadBlobAsync(imageUri);

                    await this.blobStorage.UploadAsync(
                        image, this.enviroment.GetVariable("AttachmentsContainerName"), $"dni_{person.NationalId}.png");

                    var doneeRepository = this.repositoryFactory.Create<DoneeEntity>();

                    await doneeRepository.PutAsync(
                        new DoneeEntity(person.NationalId,
                                        person.FirstName,
                                        person.LastName,
                                        person.DateOfBirth,
                                        person.Sex));

                    var actionRetry = await actionRetryRepository.GetAsync(message.SanitizeTo(), Action.RecognizeId.ToString());

                    if (actionRetry != null)
                    {
                        await actionRetryRepository.DeleteAsync(actionRetry);
                    }
                }
                else
                {
                    var actionRetry = await actionRetryRepository.GetAsync(message.SanitizeTo(), Action.RecognizeId.ToString());

                    if (actionRetry == null)
                    {
                        await actionRetryRepository.PutAsync(new ActionRetryEntity(message.SanitizeTo(), Action.RecognizeId.ToString()));
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

                            logger.LogWarning($"Unable to process national_id.{Environment.NewLine}Message:{Environment.NewLine}{message}");

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

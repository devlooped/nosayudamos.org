using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    // This workflow is only selected if there is no person registered yet,
    // since its [Workflow] attribute does not specify a role.
    [Workflow]
    class StartupWorkflow : IWorkflow
    {
        readonly IEnvironment environment;
        readonly IEventStreamAsync events;
        readonly ILanguageUnderstanding language;
        readonly IPersonalIdRecognizer idRecognizer;
        readonly ITaxIdRecognizer taxRecognizer;
        readonly DurableAction durableAction;
        readonly IMessaging messaging;
        readonly HttpClient http;
        readonly IBlobStorage blobStorage;
        readonly IPersonRepository personRepository;
        readonly ILogger<StartupWorkflow> logger;

        public StartupWorkflow(
            IEnvironment environment,
            IEventStreamAsync events,
            ILanguageUnderstanding language,
            IPersonalIdRecognizer idRecognizer,
            ITaxIdRecognizer taxRecognizer,
            DurableAction durableAction,
            IMessaging messaging,
            HttpClient http,
            IBlobStorage blobStorage, 
            IPersonRepository personRepository,
            ILogger<StartupWorkflow> logger)
            => (this.environment, this.events, this.language, this.idRecognizer, this.taxRecognizer, this.durableAction, this.messaging, this.http, this.blobStorage, this.personRepository, this.logger)
            = (environment, events, language, idRecognizer, taxRecognizer, durableAction, messaging, http, blobStorage, personRepository, logger);

        public async Task RunAsync(MessageEvent @event, Person? person)
        {
            if (!(@event is MessageReceived message))
                return;

            // If we receive an image from an unregistered number, we assume 
            // it's someone trying to register by sending their ID as requested 
            // to become a donee.
            if (Uri.TryCreate(message.Body, UriKind.Absolute, out var imageUri))
            {
                await RegisterDoneeAsync(message, imageUri);
                return;
            }

            var intents = await language.GetIntentsAsync(message.Body);
            if (intents.TryGetValue("Help", out var helpIntent) &&
                helpIntent.Score >= 0.85)
            {
                // User wants to be a donee, we need the ID
                await events.PushAsync(new MessageSent(message.To, message.From, Strings.UI.Donee.SendIdentifier));
            }
            else if (intents.TryGetValue("Donate", out var donateIntent) &&
                donateIntent.Score >= 0.85)
            {
                await events.PushAsync(new MessageSent(message.To, message.From, Strings.UI.Donor.SendAmount));
            }
            else
            {
                // Can't figure out intent, or score is too low.
                await events.PushAsync(new UnknownMessageReceived(message.From, message.To, message.Body) { When = message.When });
                await events.PushAsync(new MessageSent(message.To, message.From, Strings.UI.UnknownIntent));
            }
        }

        private async Task RegisterDoneeAsync(MessageReceived message, Uri imageUri)
        {
#pragma warning disable CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
            var id = await durableAction.ExecuteAsync(
#pragma warning restore CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
                () => idRecognizer.RecognizeAsync(imageUri),
                () => messaging.SendTextAsync(message.To, "Dni invalido, intente de nuevo.", message.From),
                () => messaging.SendTextAsync(message.To, "No pudimos procesar su dni. Nos contactaremos en breve.",
                message.From));

            if (id != null)
            {
                var image = await http.GetByteArrayAsync(message.Body);

                await blobStorage.UploadAsync(
                    image, environment.GetVariable("AttachmentsContainerName"), $"dni_{id.NationalId}.png");

                var person = await personRepository.GetAsync(id.NationalId, readOnly: false);
                if (person == null)
                {
                    person = new Person(id.NationalId, id.FirstName, id.LastName, message.From, Role.Donee, id.DateOfBirth, id.Sex);

                    var tax = await taxRecognizer.RecognizeAsync(person);
                    if (tax != null)
                        person.UpdateTaxStatus(tax);

                    await personRepository.PutAsync(person);
                }
                else
                {
                    person.UpdatePhoneNumber(message.From);
                    await personRepository.PutAsync(person);
                }
            }
        }
    }
}

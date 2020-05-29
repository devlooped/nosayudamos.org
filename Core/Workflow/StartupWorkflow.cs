using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    // This workflow is only selected if there is no person registered yet,
    // since its [Workflow] attribute does not specify a role.
    [Workflow]
    class StartupWorkflow : IWorkflow
    {
        readonly IEnvironment env;
        readonly IEventStreamAsync events;
        readonly IPersonalIdRecognizer idRecognizer;
        readonly ITaxIdRecognizer taxRecognizer;
        readonly DurableAction durableAction;
        readonly IMessaging messaging;
        readonly HttpClient http;
        readonly IBlobStorage blobStorage;
        readonly IPersonRepository peopleRepo;
        readonly ILogger<StartupWorkflow> logger;

        public StartupWorkflow(
            IEnvironment env,
            IEventStreamAsync events,
            IPersonalIdRecognizer idRecognizer,
            ITaxIdRecognizer taxRecognizer,
            DurableAction durableAction,
            IMessaging messaging,
            HttpClient http,
            IBlobStorage blobStorage,
            IPersonRepository peopleRepo,
            ILogger<StartupWorkflow> logger)
            => (this.env, this.events, this.idRecognizer, this.taxRecognizer, this.durableAction, this.messaging, this.http, this.blobStorage, this.peopleRepo, this.logger)
            = (env, events, idRecognizer, taxRecognizer, durableAction, messaging, http, blobStorage, peopleRepo, logger);

        public async Task RunAsync(MessageReceived message, Prediction prediction, Person? person)
        {
            // If we receive an image from an unregistered number, we assume 
            // it's someone trying to register by sending their ID as requested 
            // to become a donee.
            if (Uri.TryCreate(message.Body, UriKind.Absolute, out var imageUri))
            {
                await RegisterDoneeAsync(message, imageUri).ConfigureAwait(false);
                return;
            }

            Intent? intent;
            if ((prediction.Intents.TryGetValue(Intents.Utilities.Help, out intent) ||
                prediction.Intents.TryGetValue(Intents.Help, out intent)) &&
                intent.Score >= 0.85)
            {
                // User wants to be a donee, we need the ID
                await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.Donee.SendIdentifier)).ConfigureAwait(false);
            }
            else if (prediction.Intents.TryGetValue(Intents.Donate, out intent) &&
                intent.Score >= 0.85)
            {
                await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.Donor.SendAmount)).ConfigureAwait(false);
            }
            else
            {
                // Can't figure out intent, or score is too low.
                await events.PushAsync(new UnknownMessageReceived(message.PhoneNumber, message.Body) { When = message.When }).ConfigureAwait(false);
                await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.UnknownIntent)).ConfigureAwait(false);
            }
        }

        async Task RegisterDoneeAsync(MessageReceived message, Uri imageUri)
        {
            var image = imageUri.Scheme == "file" ?
                await File.ReadAllBytesAsync(imageUri.AbsolutePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)) :
                await http.GetByteArrayAsync(imageUri);

#pragma warning disable CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
            var id = await durableAction.ExecuteAsync(
#pragma warning restore CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
                async (attempt) =>
                {
                    await blobStorage.UploadAsync(
                        image, env.GetVariable("AttachmentsContainerName"), $"cel_{message.PhoneNumber}_{attempt}.png")
                        .ConfigureAwait(false);
                    return await idRecognizer.RecognizeAsync(image).ConfigureAwait(false);
                },
                attempt =>
                {
                    var value = Resources.ResourceManager.GetString("UI_Donee_ResendIdentifier" + attempt, CultureInfo.CurrentCulture)
                        ?? Strings.UI.Donee.RegistrationFailed;
                    return events.PushAsync(new MessageSent(message.PhoneNumber, value));
                },
                async (attemps) =>
                {
                    // TODO: should this be done by the RegistrationFailedHandler instead?
                    await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.Donee.RegistrationFailed)).ConfigureAwait(false);

                    var images = new List<Uri>();
                    for (int i = 1; i <= attemps; i++)
                    {
                        var uri = await blobStorage.GetUriAsync(
                            env.GetVariable("AttachmentsContainerName"),
                            $"cel_{message.PhoneNumber}_{i}.png")
                        .ConfigureAwait(false);

                        if (uri != null)
                            images.Add(uri);
                    }

                    // Since we'll be following up personally via slack, pause further automation for this number.
                    await events.PushAsync(new AutomationPaused(message.PhoneNumber, nameof(StartupWorkflow))).ConfigureAwait(false);
                    // We register the failure at the end of all attempts, to follow-up personally via Slack.
                    await events.PushAsync(new RegistrationFailed(message.PhoneNumber, images.ToArray())).ConfigureAwait(false);
                },
                nameof(RegisterDoneeAsync) + message.PhoneNumber);

            if (id != null)
            {
                await blobStorage.UploadAsync(
                        image,
                        env.GetVariable("AttachmentsContainerName"), $"dni_{id.NationalId}.png")
                    .ConfigureAwait(false);

                var person = await peopleRepo.GetAsync(id.NationalId, readOnly: false).ConfigureAwait(false);
                if (person == null)
                {
                    person = new Person(id.NationalId, id.FirstName, id.LastName, message.PhoneNumber, Role.Donee, id.DateOfBirth, id.Sex);

                    var tax = await taxRecognizer.RecognizeAsync(person).ConfigureAwait(false);
                    if (tax != null)
                        person.UpdateTaxStatus(tax);

                    await peopleRepo.PutAsync(person).ConfigureAwait(false);
                }
                else
                {
                    person.UpdatePhoneNumber(message.PhoneNumber);
                    await peopleRepo.PutAsync(person).ConfigureAwait(false);
                }
            }
        }
    }
}

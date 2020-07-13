using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        readonly IEnvironment env;
        readonly IEventStreamAsync events;
        readonly IPersonalIdRecognizer idRecognizer;
        readonly ITaxIdRecognizer taxRecognizer;
        readonly DurableAction durableAction;
        readonly IMessaging messaging;
        readonly HttpClient http;
        readonly IBlobStorage blobStorage;
        readonly IEntityRepository<PhoneEntry> phoneDir;
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
            IEntityRepository<PhoneEntry> phoneDir,
            IPersonRepository peopleRepo,
            ILogger<StartupWorkflow> logger)
            => (this.env, this.events, this.idRecognizer, this.taxRecognizer, this.durableAction, this.messaging, this.http, this.blobStorage, this.phoneDir, this.peopleRepo, this.logger)
            = (env, events, idRecognizer, taxRecognizer, durableAction, messaging, http, blobStorage, phoneDir, peopleRepo, logger);

        public async Task RunAsync(PhoneEntry phone, MessageReceived message, TextAnalysis analysis, Person? person)
        {
            // If we receive an image from an unregistered number, we assume 
            // it's someone trying to register by sending their ID as requested 
            // to become a donee.
            // TODO: ignore moderated content
            if (Uri.TryCreate(message.Body, UriKind.Absolute, out var imageUri))
            {
                await RegisterDoneeAsync(message, imageUri).ConfigureAwait(false);
                return;
            }

            if (analysis.Prediction.IsIntent(Intents.Utilities.Help, Intents.Help))
            {
                phone.Role = Role.Donee;
                await phoneDir.PutAsync(phone);
                // User wants to be a donee, we need the ID
                await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.Donee.SendIdentifier)).ConfigureAwait(false);
            }
            else if (analysis.Prediction.IsIntent(Intents.Donate))
            {
                phone.Role = Role.Donor;
                await phoneDir.PutAsync(phone);
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

            var id = await durableAction.ExecuteAsync(
                // We explicitly use the person phone number as the partition key 
                // instead of the method name, which should make it easier to spot 
                // retry attempts by user, instead.
                actionName: message.PhoneNumber,
                actionId: nameof(RegisterDoneeAsync),
                async (attempts) =>
                {
                    await blobStorage.UploadAsync(
                        image, env.GetVariable("AttachmentsContainerName"), $"cel_{message.PhoneNumber}_{attempts}.png")
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
                    for (var i = 1; i <= attemps; i++)
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
                });

            if (id != null)
            {
                await blobStorage.UploadAsync(
                        image,
                        env.GetVariable("AttachmentsContainerName"), $"dni_{id.NationalId}.png")
                    .ConfigureAwait(false);

                var person = await peopleRepo.GetAsync<Donee>(id.NationalId, readOnly: false).ConfigureAwait(false);
                if (person == null)
                {
                    person = new Donee(id.NationalId, id.FirstName, id.LastName, message.PhoneNumber, id.DateOfBirth, id.Sex);

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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.AI.TextAnalytics;

namespace NosAyudamos
{
    [Workflow(nameof(Role.Donee))]
    class DoneeWorkflow : IWorkflow
    {
        readonly IEnvironment env;
        readonly IEventStreamAsync events;
        readonly IPersonRepository peopleRepo;
        readonly IRequestRepository helpRepo;
        readonly IPersonalIdRecognizer idRecognizer;
        readonly ITaxIdRecognizer taxRecognizer;
        readonly HttpClient http;
        readonly IDurableAction durableAction;
        readonly IBlobStorage blobStorage;

        public DoneeWorkflow(
            IEnvironment env,
            IEventStreamAsync events,
            IPersonRepository peopleRepo,
            IRequestRepository helpRepo,
            IPersonalIdRecognizer idRecognizer,
            ITaxIdRecognizer taxRecognizer,
            HttpClient http,
            IDurableAction durableAction,
            IBlobStorage blobStorage)
            => (this.env, this.events, this.peopleRepo, this.helpRepo, this.idRecognizer, this.taxRecognizer, this.http, this.durableAction, this.blobStorage)
            = (env, events, peopleRepo, helpRepo, idRecognizer, taxRecognizer, http, durableAction, blobStorage);

        public async Task RunAsync(PhoneEntry phone, MessageReceived message, TextAnalysis analysis, Person? person)
        {
            // TODO: ignore moderated content
            if (person == null && Uri.TryCreate(message.Body, UriKind.Absolute, out var imageUri))
            {
                await RegisterAsync(message, imageUri).ConfigureAwait(false);
                return;
            }

            if (person == null ||
                !(person is Donee donee))
                return;

            if (analysis.Prediction.IsIntent(Intents.Instructions))
            {
                await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.Donee.Instructions));
                return;
            }

            if (analysis.Prediction.IsIntent(Intents.Help))
            {
                var quantities = analysis.Entities.Where(e => e.Category == EntityCategory.Quantity).ToList();
                if (quantities.Count == 0)
                {
                    // TODO: need amount
                    return;
                }

                CategorizedEntity? quantity = quantities.Count == 1 ? quantities[0] :
                    quantities.FirstOrDefault(e => e.SubCategory == "Currency");

                if (quantity == null)
                {
                    // TODO: multiple quantities, need one amount
                    return;
                }

                // TODO: for the above cases, as well as further clarification interactions 
                // to polish the publication before submission, we 

                //var request = donee.RequestHelp(int.Parse(quantity.Value.Text), message.Body);


                return;
            }

            // TODO: else?
        }

        async Task RegisterAsync(MessageReceived message, Uri imageUri)
        {
            var image = imageUri.Scheme == "file" ?
                await File.ReadAllBytesAsync(imageUri.AbsolutePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)) :
                await http.GetByteArrayAsync(imageUri);

            var id = await durableAction.ExecuteAsync(
                // We explicitly use the person phone number as the partition key 
                // instead of the method name, which should make it easier to spot 
                // retry attempts by user, instead.
                actionName: message.PhoneNumber,
                actionId: nameof(RegisterAsync),
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

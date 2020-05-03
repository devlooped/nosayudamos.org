using System;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using NosAyudamos.Properties;
using Serilog;

namespace NosAyudamos.Functions
{
    /// <summary>
    /// Initial handler of uncategorized incoming messages from event grid 
    /// callbacks into our azure function. Made testable by implementing 
    /// <see cref="IEventHandler{TEvent}"/>.
    /// </summary>
    class Inbox : IEventHandler<MessageReceived>
    {
        readonly ILogger log;
        readonly ISerializer serializer;
        readonly IPersonRepository repository;
        readonly ILanguageUnderstanding language;
        readonly IEventStreamAsync events;

        public Inbox(ILogger log, ISerializer serializer, ILanguageUnderstanding language, IPersonRepository repository, IEventStreamAsync events)
            => (this.log, this.serializer, this.language, this.repository, this.events) 
            = (log, serializer, language, repository, events);

        [FunctionName("inbox")]
        public Task RunAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<MessageReceived>(serializer));

        public async Task HandleAsync(MessageReceived message)
        {
            log.Verbose("{@Message:j}", message);

            // Performs minimal discovery of existing person id (if any)
            // and whether it's a text or image message.
            var person = await repository.FindAsync(message.From);
            var id = person?.Id;
            Uri? imageUri = default;
            Uri.TryCreate(message.Body, UriKind.Absolute, out imageUri);

            // Person is still not registered, need to discover intent
            if (person == null)
            {
                var intents = await language.GetIntentsAsync(message.Body);
                if (intents.TryGetValue("help", out var helpIntent) &&
                    helpIntent.Score >= 0.85)
                {
                    // User wants to be a donee, we need the ID
                    await events.PushAsync(new MessageSent(message.To, message.From, Strings.UI.Donee.SendIdentifier));
                }
                else if (intents.TryGetValue("donate", out var donateIntent) &&
                    donateIntent.Score >= 0.85)
                {
                    await events.PushAsync(new MessageSent(message.To, message.From, Strings.UI.Donor.SendAmount));
                }
                else
                {
                    // Can't figure out intent, or score is to low.
                    await events.PushAsync(new UnknownMessageReceived(message.From, message.To, message.Body) { When = message.When });
                    await events.PushAsync(new MessageSent(message.To, message.From, Strings.UI.UnknownIntent));
                }
            }
            else
            {
                var intents = await language.GetIntentsAsync(message.Body);

                if (intents.ContainsKey("None"))
                {
                    if (person.Role == Role.Donee)
                        await events.PushAsync(new MessageSent(message.To, message.From, Strings.UI.UnknownIntent));
                    else
                        await events.PushAsync(new MessageSent(message.To, message.From, Strings.UI.Donor.SendAmount));
                }

                // TODO load worklow for person, run it.
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using Serilog;

namespace NosAyudamos
{
    /// <summary>
    /// Initial handler of uncategorized incoming messages from event grid 
    /// callbacks into our azure function. Made testable by implementing 
    /// <see cref="IEventHandler{TEvent}"/>.
    /// </summary>
    class MessageReceivedHandler : IEventHandler<MessageReceived>
    {
        readonly ILogger log;
        readonly ISerializer serializer;
        readonly IPersonRepository personRepo;
        readonly IEntityRepository<PhoneSystem> phoneRepo;
        readonly ILanguageUnderstanding language;
        readonly IEventStreamAsync events;
        readonly IWorkflowSelector selector;

        public MessageReceivedHandler(
            ILogger log, ISerializer serializer, ILanguageUnderstanding language, 
            IPersonRepository personRepo, IEntityRepository<PhoneSystem> phoneRepo,
            IEventStreamAsync events, IWorkflowSelector selector)
            => (this.log, this.serializer, this.language, this.personRepo, this.phoneRepo, this.events, this.selector)
            = (log, serializer, language, personRepo, phoneRepo, events, selector);

        public async Task HandleAsync(MessageReceived message)
        {
            log.Verbose("{@Message:j}", message);

            // Always update the map of user phone > system phone 
            // So we can reliably reply to a user from their preferred system phone #.
            await phoneRepo.PutAsync(new PhoneSystem(message.From, message.To)).ConfigureAwait(false);

            // Performs minimal discovery of existing person id (if any)
            // and whether it's a text or image message.
            var person = await personRepo.FindAsync(message.From).ConfigureAwait(false);
            var workflow = selector.Select(person?.Role);

            await workflow.RunAsync(message, person).ConfigureAwait(false);

            return;
        }
    }
}

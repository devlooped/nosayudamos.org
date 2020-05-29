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
        readonly IPersonRepository peopleRepo;
        readonly ILanguageUnderstanding language;
        readonly IEntityRepository<PhoneSystem> phoneDir;
        readonly IWorkflowSelector selector;

        public MessageReceivedHandler(
            ILogger log,
            IPersonRepository peopleRepo, 
            ILanguageUnderstanding language,
            IEntityRepository<PhoneSystem> phoneDir,
            IWorkflowSelector selector)
            => (this.log, this.peopleRepo, this.language, this.phoneDir, this.selector)
            = (log, peopleRepo, language, phoneDir, selector);

        public async Task HandleAsync(MessageReceived message)
        {
            log.Verbose("{@Message:j}", message);

            var phoneSystem = await phoneDir.GetAsync(message.PhoneNumber).ConfigureAwait(false);
            if (phoneSystem == null)
            {
                // Always update the map of user phone > system phone 
                // So we can reliably reply to a user from their preferred system phone #.
                await phoneDir.PutAsync(new PhoneSystem(message.PhoneNumber, message.SystemNumber)).ConfigureAwait(false);
            }
            else if (phoneSystem.SystemNumber != message.SystemNumber)
            {
                phoneSystem.SystemNumber = message.SystemNumber;
                await phoneDir.PutAsync(phoneSystem);
            }

            // If automation has been paused for this user, don't perform any subsequent processing.
            if (phoneSystem?.AutomationPaused == true)
                return;

            // Performs minimal discovery of existing person id (if any) and intents
            var person = await peopleRepo.FindAsync(message.PhoneNumber).ConfigureAwait(false);
            var workflow = selector.Select(person?.Role);
            var prediction = await language.PredictAsync(message.Body);

            await workflow.RunAsync(message, prediction, person).ConfigureAwait(false);
        }
    }
}

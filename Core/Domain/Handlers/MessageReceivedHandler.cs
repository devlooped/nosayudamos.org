using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    /// <summary>
    /// Initial handler of uncategorized incoming messages from event grid 
    /// callbacks into our azure function. Made testable by implementing 
    /// <see cref="IEventHandler{TEvent}"/>.
    /// </summary>
    class MessageReceivedHandler : IEventHandler<MessageReceived>
    {
        readonly ILogger<MessageReceivedHandler> log;
        readonly IPersonRepository peopleRepo;
        readonly ILanguageUnderstanding language;
        readonly ITextAnalyzer textAnalyzer;
        readonly IEntityRepository<PhoneEntry> phoneDir;
        readonly IWorkflowSelector selector;

        public MessageReceivedHandler(
            ILogger<MessageReceivedHandler> log,
            IPersonRepository peopleRepo,
            ILanguageUnderstanding language,
            ITextAnalyzer textAnalyzer,
            IEntityRepository<PhoneEntry> phoneDir,
            IWorkflowSelector selector)
            => (this.log, this.peopleRepo, this.language, this.textAnalyzer, this.phoneDir, this.selector)
            = (log, peopleRepo, language, textAnalyzer, phoneDir, selector);

        public async Task HandleAsync(MessageReceived message)
        {
            log.LogInformation("{@Message:j}", message);

            var phoneEntry = await phoneDir.GetAsync(message.PhoneNumber).ConfigureAwait(false);
            if (phoneEntry == null)
            {
                // Always update the map of user phone > system phone 
                // So we can reliably reply to a user from their preferred system phone #.
                phoneEntry = await phoneDir.PutAsync(new PhoneEntry(message.PhoneNumber, message.SystemNumber)).ConfigureAwait(false);
            }
            else if (phoneEntry.SystemNumber != message.SystemNumber)
            {
                phoneEntry.SystemNumber = message.SystemNumber;
                phoneEntry = await phoneDir.PutAsync(phoneEntry);
            }

            // If automation has been paused for this user, don't perform any subsequent processing.
            if (phoneEntry!.AutomationPaused == true)
                return;

            // Performs minimal discovery of existing person id (if any) and intents
            var person = await peopleRepo.FindAsync(message.PhoneNumber).ConfigureAwait(false);
            var workflow = selector.Select(person?.Role);
            var prediction = await language.PredictAsync(message.Body);
            var entities = await textAnalyzer.GetEntitiesAsync(message.Body);
            var keyPhrases = await textAnalyzer.GetKeyPhrasesAsync(message.Body);

            await workflow.RunAsync(phoneEntry, message, new TextAnalysis(prediction, entities.ToArray(), keyPhrases.ToArray()), person).ConfigureAwait(false);
        }
    }
}

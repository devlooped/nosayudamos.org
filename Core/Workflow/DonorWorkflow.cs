using System.Threading.Tasks;

namespace NosAyudamos
{
    [Workflow(nameof(Role.Donor))]
    class DonorWorkflow : IWorkflow
    {
        readonly IEventStreamAsync events;

        public DonorWorkflow(IEventStreamAsync events) => this.events = events;

        public async Task RunAsync(MessageReceived message, TextAnalysis analysis, Person? person)
        {
            if (person == null)
                return;

            if (analysis.Prediction.IsIntent(Intents.Instructions))
            {
                await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.Donor.Instructions));
                return;
            }
        }
    }
}

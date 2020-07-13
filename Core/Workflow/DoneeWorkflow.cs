using System.Linq;
using System.Threading.Tasks;
using Azure.AI.TextAnalytics;

namespace NosAyudamos
{
    [Workflow(nameof(Role.Donee))]
    class DoneeWorkflow : IWorkflow
    {
        readonly IEventStreamAsync events;
        readonly IPersonRepository peopleRepo;
        readonly IRequestRepository helpRepo;

        public DoneeWorkflow(IEventStreamAsync events, IPersonRepository peopleRepo, IRequestRepository helpRepo)
        {
            this.events = events;
            this.peopleRepo = peopleRepo;
            this.helpRepo = helpRepo;
        }

        public async Task RunAsync(PhoneEntry phone, MessageReceived message, TextAnalysis analysis, Person? person)
        {
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
    }
}

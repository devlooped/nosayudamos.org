using System.Threading.Tasks;
using Merq;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    [Workflow(nameof(Role.Donee))]
    class DoneeWorkflow : IWorkflow
    {
        readonly IEventStreamAsync events;

        public DoneeWorkflow(IEventStreamAsync events) => this.events = events;

        public async Task RunAsync(MessageReceived message, Prediction prediction, Person? person)
        {
            if (person == null)
                return;

            if (prediction.TopIntent == Intents.Instructions && 
                prediction.Intents.TryGetValue(Intents.Instructions, out var intent) && 
                intent.Score >= 0.85)
            {
                await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.Donee.Instructions));
                return;
            }
        }
    }
}

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.EventGrid.Models;

namespace NosAyudamos.Functions
{
    class SlackOutgoing
    {
        readonly ISerializer serializer;
        readonly SlackUnknownMessageReceivedHandler handler;

        public SlackOutgoing(ISerializer serializer, SlackUnknownMessageReceivedHandler handler)
            => (this.serializer, this.handler)
            = (serializer, handler);

        [FunctionName("slack_outgoing")]
        public Task HandleUnknownIntentAsync([EventGridTrigger] EventGridEvent e) => handler.HandleAsync(e.GetData<UnknownMessageReceived>(serializer));
    }
}

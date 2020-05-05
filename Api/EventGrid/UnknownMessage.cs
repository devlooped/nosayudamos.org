using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.EventGrid.Models;

namespace NosAyudamos.EventGrid
{
    class UnknownMessage
    {
        readonly ISerializer serializer;
        readonly SlackUnknownMessageReceivedHandler handler;

        public UnknownMessage(ISerializer serializer, SlackUnknownMessageReceivedHandler handler)
            => (this.serializer, this.handler)
            = (serializer, handler);

        [FunctionName("unknown_message")]
        public Task HandleUnknownIntentAsync([EventGridTrigger] EventGridEvent e) => handler.HandleAsync(e.GetData<UnknownMessageReceived>(serializer));
    }
}

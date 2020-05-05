using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace NosAyudamos.Functions
{
    class MessageOutgoing
    {
        readonly MessageSentHandler handler;
        readonly ISerializer serializer;

        public MessageOutgoing(MessageSentHandler handler, ISerializer serializer)
            => (this.handler, this.serializer) 
            = (handler, serializer);

        [FunctionName("message_outgoing")]
        public Task RunAsync([EventGridTrigger] EventGridEvent e) => handler.HandleAsync(e.GetData<MessageSent>(serializer));
    }
}

using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using NosAyudamos.Events;

namespace NosAyudamos.Functions
{
    class MessageOutgoing : IEventHandler<MessageSent>
    {
        readonly IMessaging messaging;
        readonly ISerializer serializer;

        public MessageOutgoing(IMessaging messaging, ISerializer serializer)
            => (this.messaging, this.serializer) = (messaging, serializer);

        [FunctionName("message_outgoing")]
        public Task RunAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<MessageSent>(serializer));

        public Task HandleAsync(MessageSent e) => messaging.SendTextAsync(e.From, e.Body, e.To);
    }
}

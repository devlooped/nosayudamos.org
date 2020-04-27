using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using NosAyudamos.Events;

namespace NosAyudamos.Functions
{
    /// <summary>
    /// Initial handler of uncategorized incoming messages from event grid 
    /// callbacks into our azure function. Made testable by implementing 
    /// <see cref="IEventHandler{TEvent}"/>.
    /// </summary>
    class Sender : IEventHandler<MessageSent>
    {
        readonly IMessaging messaging;
        readonly ISerializer serializer;

        public Sender(IMessaging messaging, ISerializer serializer)
            => (this.messaging, this.serializer) = (messaging, serializer);

        [FunctionName("sender")]
        public Task RunAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(serializer.Deserialize<MessageSent>(e.Data));

        public Task HandleAsync(MessageSent e) => messaging.SendTextAsync(e.From, e.Body, e.To);
    }
}

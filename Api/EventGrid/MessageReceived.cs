using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace NosAyudamos.EventGrid
{
    /// <summary>
    /// Initial handler of uncategorized incoming messages from event grid 
    /// callbacks into our azure function. Made testable by implementing 
    /// <see cref="IEventHandler{TEvent}"/>.
    /// </summary>
    class MessageReceived
    {
        readonly MessageReceivedHandler handler;
        readonly ISerializer serializer;

        public MessageReceived(MessageReceivedHandler handler, ISerializer serializer)
            => (this.handler, this.serializer)
            = (handler, serializer);

        [FunctionName("inbox")]
        public Task RunAsync([EventGridTrigger] EventGridEvent e) => handler.HandleAsync(e.GetData<NosAyudamos.MessageReceived>(serializer));
    }
}

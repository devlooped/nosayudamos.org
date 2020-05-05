using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace NosAyudamos.EventGrid
{
    class MessageSent
    {
        readonly MessageSentHandler handler;
        readonly ISerializer serializer;

        public MessageSent(MessageSentHandler handler, ISerializer serializer)
            => (this.handler, this.serializer)
            = (handler, serializer);

        [FunctionName("message-sent")]
        public Task RunAsync([EventGridTrigger] EventGridEvent e) => handler.HandleAsync(e.GetData<NosAyudamos.MessageSent>(serializer));
    }
}

using System.Threading.Tasks;

namespace NosAyudamos
{
    class MessageSentHandler : IEventHandler<MessageSent>
    {
        readonly IMessaging messaging;

        public MessageSentHandler(IMessaging messaging) => this.messaging = messaging;

        public Task HandleAsync(MessageSent e) => messaging.SendTextAsync(e.From, e.Body, e.To);
    }
}

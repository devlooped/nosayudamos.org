namespace NosAyudamos
{
    public class RequestReplied : DomainEvent
    {
        public RequestReplied(string senderId, string message)
            => (SenderId, Message)
            = (senderId, message);

        public string SenderId { get; }
        public string Message { get; }
    }
}

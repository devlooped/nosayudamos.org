namespace NosAyudamos
{
    public class MessageReceived : MessageEvent
    {
        public MessageReceived(string phoneNumber, string systemNumber, string body) : base(phoneNumber)
            => (Body, SystemNumber)
            = (body, systemNumber);

        public string SystemNumber { get; }

        public string Body { get; }
    }
}

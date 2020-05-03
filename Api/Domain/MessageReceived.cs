namespace NosAyudamos
{
    public class MessageReceived : MessageEvent
    {
        public MessageReceived(string from, string to, string body) : base(from, to)
            => Body = body;

        public string Body { get; }
    }
}

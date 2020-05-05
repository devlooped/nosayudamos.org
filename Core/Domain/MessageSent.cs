namespace NosAyudamos
{
    public class MessageSent : MessageEvent
    {
        public MessageSent(string from, string to, string body) : base(from, to)
            => Body = body;

        public string Body { get; }
        public string? PersonId { get; set; }
    }
}

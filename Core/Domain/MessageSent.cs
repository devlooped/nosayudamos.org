namespace NosAyudamos
{
    public class MessageSent : MessageEvent
    {
        public MessageSent(string to, string body) : base(to)
            => Body = body;

        public string Body { get; }
        public string? PersonId { get; set; }
    }
}

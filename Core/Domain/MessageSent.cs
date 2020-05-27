namespace NosAyudamos
{
    public class MessageSent : MessageEvent
    {
        public MessageSent(string phoneNumber, string body) : base(phoneNumber)
            => Body = body;

        public string Body { get; }
        public string? PersonId { get; set; }
    }
}

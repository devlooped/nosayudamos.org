namespace NosAyudamos
{
    class Requested : DomainEvent
    {
        public Requested(string requestId, int amount, string description, string[] keywords)
            => (RequestId, Amount, Description, Keywords)
            = (requestId, amount, description, keywords);

        public string RequestId { get; }
        public int Amount { get; }
        public string Description { get; }
        public string[] Keywords { get; }
    }
}

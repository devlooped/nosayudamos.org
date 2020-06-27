using System;

namespace NosAyudamos
{
    class RequestCreated : DomainEvent
    {
        public RequestCreated(string personId, int amount, string description, string[] keywords, string? requestId = default)
            => (PersonId, Amount, Description, Keywords, RequestId)
            = (personId, amount, description, keywords, requestId ?? Guid.NewGuid().ToString("n"));

        public string PersonId { get; }
        public string RequestId { get; }
        public int Amount { get; }
        public string Description { get; }
        public string[] Keywords { get; }
    }
}

using System;
using Newtonsoft.Json;

namespace NosAyudamos
{
    class RequestCreated : DomainEvent
    {
        [JsonConstructor]
        public RequestCreated(string requestId, int amount, string description, string[]? keywords = default, string? personId = default)
        {
            RequestId = requestId;
            Amount = amount;
            Description = description;
            Keywords = keywords ?? Array.Empty<string>();
            PersonId = personId ?? requestId.Substring(0, requestId.IndexOf('-', StringComparison.Ordinal));
        }

        [JsonIgnore]
        public string PersonId { get; }
        public string RequestId { get; }
        public int Amount { get; }
        public string Description { get; }
        public string[] Keywords { get; }
    }
}

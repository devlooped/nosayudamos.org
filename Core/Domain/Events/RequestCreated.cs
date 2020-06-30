using System;
using Newtonsoft.Json;

namespace NosAyudamos
{
    class RequestCreated : DomainEvent
    {
        // Start counting from some recent memorable date, like when we started the quarantine...
        static readonly DateTimeOffset quarantine = new DateTimeOffset(2020, 3, 20, 0, 0, 0,
            TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time").BaseUtcOffset);

        [JsonConstructor]
        public RequestCreated(string requestId, int amount, string description, string[]? keywords = default)
        {
            PersonId = requestId.Substring(0, requestId.IndexOf('-', StringComparison.Ordinal));
            RequestId = requestId;
            Amount = amount;
            Description = description;
            Keywords = keywords ?? Array.Empty<string>();
        }

        public RequestCreated(string personId, int amount, string description, string[]? keywords = default, int personVersion = 1)
            => (PersonId, Amount, Description, Keywords, RequestId)
            = (personId, amount, description, keywords ?? Array.Empty<string>(), NewRequestId(personId, personVersion));

        [JsonIgnore]
        public string PersonId { get; }
        public string RequestId { get; }
        public int Amount { get; }
        public string Description { get; }
        public string[] Keywords { get; }

        /// <summary>
        /// Creates an automatic request identifier when none is specified.
        /// </summary>
        /// <remarks>
        /// Since the identifier only needs to be unique for a given person ID, and it's highly 
        /// unlikely that said person can create two requests within the same second, that's 
        /// mostly guaranteed to be unique for production use. In order to make it even more 
        /// robust, we use the person version (event # in its history) too.
        /// <para>
        /// Sample generated ids are 20000000-7P01, 20000000-6AV1 and 20000000-5HrA.
        /// </para>
        /// </remarks>
        static string NewRequestId(string personId, int personVersion)
            => personId + "-" + Base62.Encode((long)((DateTimeOffset.UtcNow - quarantine).TotalSeconds / (personVersion <= 0 ? 1 : personVersion)));
    }
}

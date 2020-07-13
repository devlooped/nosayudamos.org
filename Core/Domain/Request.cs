#pragma warning disable CS8618 // Non-nullable field is uninitialized. The pattern is intentional for an event-sourced domain object.
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NosAyudamos
{
    class Request : DomainObject, IIdentifiable
    {
        string requestId;

        public Request(IEnumerable<DomainEvent> history)
            : this() => Load(history);

        public Request(
            string personId,
            int amount,
            string description,
            string[]? keywords = default)
            : this()
        {
            IsReadOnly = false;
            // Requests only need to be unique within a person, so we use that as the prefix to disambiguate 
            // multiple requests at the same UtcNow. The high-precision time also helps make that unique too.
            Raise(new RequestCreated(personId + "-" + Base62.Encode(PreciseTime.UtcNow.Ticks), amount, description, keywords, personId: personId));
        }

        Request()
        {
            Handles<RequestCreated>(OnCreated);
            Handles<RequestReplied>(OnReplied);
        }

        /// <summary>
        /// The <see cref="RequestId"/> is sufficiently unique, since it 
        /// contains both the <see cref="PersonId"/> in addition to the request 
        /// identifier itself, so we can safely use that to generate unique 
        /// event identifiers.
        /// </summary>
        string IIdentifiable.Id => RequestId;

        // NOTE: the [JsonProperty] attributes allow the deserialization from 
        // JSON to be able to set the properties when loading from the last  
        // saved known snapshot state.

        [JsonIgnore]
        public string PersonId { get; private set; }

        [JsonProperty]
        public string RequestId
        {
            get => requestId;
            private set
            {
                requestId = value;
                // When processing OnCreated(RequestCreated), we'll have already set PersonId, so no need to re-parse it
                if (string.IsNullOrEmpty(PersonId) || !requestId.StartsWith(PersonId, StringComparison.Ordinal))
                {
                    // By setting this via the setter, we can avoid serializing the PersonId explicitly.
                    PersonId = requestId.Substring(0, requestId.IndexOf('-', StringComparison.Ordinal));
                }
            }
        }

        /// <summary>
        /// The requested amount.
        /// </summary>
        [JsonProperty]
        public int Amount { get; private set; }

        /// <summary>
        /// Description for the request.
        /// </summary>
        [JsonProperty]
        public string Description { get; private set; }

        [JsonProperty]
        public string[] Keywords { get; private set; }

        [JsonProperty]
        public List<MessageData> Messages { get; private set; } = new List<MessageData>();

        public void Reply(string senderId, string message)
            // TODO: something to validate?
            => Raise(new RequestReplied(senderId, message));

        void OnCreated(RequestCreated created)
            => (PersonId, RequestId, Amount, Description, Keywords)
            = (created.PersonId, created.RequestId, created.Amount, created.Description, created.Keywords);

        void OnReplied(RequestReplied reply)
            => Messages.Add(new MessageData(reply.SenderId, reply.Message));

        public class MessageData
        {
            public MessageData(string senderId, string message)
                => (SenderId, Message)
                = (senderId, message);
            public string SenderId { get; }
            public string Message { get; }
            public override int GetHashCode() => (SenderId, Message).GetHashCode();
            public override bool Equals(object obj)
                => obj is MessageData data && data.SenderId == SenderId && data.Message == Message;
        }
    }
}

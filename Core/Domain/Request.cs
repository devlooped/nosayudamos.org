#pragma warning disable CS8618 // Non-nullable field is uninitialized. The pattern is intentional for an event-sourced domain object.
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NosAyudamos
{
    class Request : DomainObject
    {
        public Request(IEnumerable<DomainEvent> history)
            : this() => Load(history);

        public Request(
            string personId,
            int amount,
            string description,
            string[] keywords)
            : this()
        {
            IsReadOnly = false;
            Raise(new RequestCreated(personId, amount, description, keywords));
        }

        Request()
        {
            Handles<RequestCreated>(OnCreated);
            Handles<RequestReplied>(OnReplied);
        }

        // NOTE: the [JsonProperty] attributes allow the deserialization from 
        // JSON to be able to set the properties when loading from the last  
        // saved known snapshot state.

        [JsonProperty]
        public string PersonId { get; private set; }

        [JsonProperty]
        public string RequestId { get; private set; }

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

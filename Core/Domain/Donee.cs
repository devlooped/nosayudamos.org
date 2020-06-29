using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NosAyudamos
{
    class Donee : Person
    {
        public Donee(IEnumerable<DomainEvent> history)
            : this() => Load(history);

        public Donee(
            string id,
            string firstName,
            string lastName,
            string phoneNumber,
            DateTime? dateOfBirth = default,
            Sex? sex = default)
            : base(id, firstName, lastName, phoneNumber, Role.Donee, dateOfBirth, sex) =>
            // Base ctor cannot invoke our parameterless ctor, so we need to 
            // invoke same Init here
            Init();

        Donee() : base() => Init();

        public Request Request(int amount, string description, string[] keywords)
        {
            if (TaxStatus != TaxStatus.Validated &&
                TaxStatus != TaxStatus.Approved)
                // TODO: is this the right way to handle this scenario? 
                // How does the caller respond to this?
                throw new InvalidOperationException();

            // TODO: validate that there isn't another ongoing request, 
            // that we're not suspended, etc.

            var request = new Request(Id, amount, description, keywords);

            Raise(new Requested(request.RequestId, amount, description, keywords));

            return request;
        }

        public void Receive(Donation donation)
        {
            var request = Requests.FirstOrDefault(x => x.RequestId == donation.RequestId);
            if (request == null)
                throw new ArgumentException("Request not found for the donation.");

            //if (request.)
        }

        [JsonProperty]
        public long TotalRequested { get; private set; }

        [JsonProperty]
        public long TotalReceived { get; private set; }

        [JsonProperty]
        public long TotalSpent { get; private set; }

        [JsonProperty]
        public List<RequestData> Requests { get; private set; } = new List<RequestData>();

        void Init() => Handles<Requested>(OnRequested);

        void OnRequested(Requested requested)
        {
            TotalRequested += requested.Amount;
            Requests.Add(new RequestData(requested.RequestId, requested.Amount, requested.Description));
        }

        //void OnHelpReceived(HelpReceived received) => TotalReceived += received.Amount;

        public class RequestData
        {
            public RequestData(string requestId, int amount, string description)
                => (RequestId, Amount, Description)
                = (requestId, amount, description);
            public int Amount { get; }
            public string Description { get; }
            public string RequestId { get; }
            public override int GetHashCode() => (RequestId, Amount, Description).GetHashCode();
            public override bool Equals(object obj)
                => obj is RequestData data && data.RequestId == RequestId && data.Amount == Amount && data.Description == Description;
        }
    }
}

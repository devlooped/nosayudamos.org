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
            : base(id, firstName, lastName, phoneNumber, Role.Donee, dateOfBirth, sex)
        {
        }

        Donee() : base() => Handles<Requested>(OnHelpRequested);

        public Request Request(int amount, string description, string[] keywords)
        {
            if (TaxStatus != TaxStatus.Validated &&
                TaxStatus != TaxStatus.Approved)
                // TODO: is this the right way to handle this scenario? 
                // How does the caller respond to this?
                throw new InvalidOperationException();

            // TODO: validate that there isn't another ongoing request, 
            // that we're not suspended, etc.

            var request = new Request(amount, description, keywords);

            Raise(new Requested(request.Id, amount, description, keywords));

            return request;
        }

        public void Receive(Donation donation)
        {
            var request = Requests.FirstOrDefault(x => x.RequestId == donation.RequestId);
            if (request == null)
                throw new ArgumentException();

            //if (request.)

        }

        [JsonProperty]
        public long TotalReceived { get; private set; }

        [JsonProperty]
        public long TotalRequested { get; private set; }

        [JsonProperty]
        public List<RequestInfo> Requests { get; private set; } = new List<RequestInfo>();

        void OnHelpRequested(Requested requested)
        {
            TotalRequested += requested.Amount;
            Requests.Add(new RequestInfo(requested.RequestId, requested.Amount, requested.Description));
        }

        //void OnHelpReceived(HelpReceived received) => TotalReceived += received.Amount;
    }
}

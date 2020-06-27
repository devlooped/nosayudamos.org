using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NosAyudamos
{
    class Donor : Person
    {
        public Donor(IEnumerable<DomainEvent> history)
            : this() => Load(history);

        public Donor(
            string id,
            string firstName,
            string lastName,
            string phoneNumber,
            DateTime? dateOfBirth = default,
            Sex? sex = default)
            : base(id, firstName, lastName, phoneNumber, Role.Donor, dateOfBirth, sex)
        {
        }

        Donor() : base() => Handles<Donated>(OnDonated);

        [JsonProperty]
        public long TotalDonated { get; private set; }

        public void Donate(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Can only donate positive amounts.");

            Raise(new Donated(amount));
        }

        void OnDonated(Donated e) => TotalDonated += e.Amount;
    }
}

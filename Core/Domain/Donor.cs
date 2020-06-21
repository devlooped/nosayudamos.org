using System;
using System.Collections.Generic;

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
            Raise(new TaxStatusApproved(Strings.Person.DonorAlwaysApproved));
        }

        Donor() : base() { }
    }
}

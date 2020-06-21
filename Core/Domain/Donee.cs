using System;
using System.Collections.Generic;

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

        Donee() : base() => Handles<HelpRequested>(OnHelpRequested);

        void OnHelpRequested(HelpRequested help)
        {

        }
    }
}

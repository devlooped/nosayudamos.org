#pragma warning disable CS8618 // Non-nullable field is uninitialized. The pattern is intentional for an event-sourced domain object.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace NosAyudamos
{
    class Person : DomainObject
    {
        public Person(IEnumerable<DomainEvent> history)
            : this() => Load(history);

        public Person(
            string id,
            string firstName,
            string lastName,
            string phoneNumber,
            string? dateOfBirth = default,
            string? sex = default)
            : this() =>
            // TODO: validate args.
            Raise(new PersonRegistered(id, firstName, lastName, phoneNumber, dateOfBirth, sex));

        /// <summary>
        /// Deserialization ctor for readonly quick loading from repository
        /// </summary>
        [JsonConstructor]
        [SuppressMessage("Design", "IDE0051:Remove unused private members", Justification = "Deserialization constructor.")]
        Person(
            string id,
            string firstName,
            string lastName,
            string phoneNumber,
            int state,
            string? dateOfBirth = default,
            string? sex = default,
            Role role = Role.Donee,
            double donatedAmount = 0)
            : this()
            => (Id, FirstName, LastName, PhoneNumber, State, DateOfBirth, Sex, Role, DonatedAmount, IsReadOnly)
            = (id, firstName, lastName, phoneNumber, state, dateOfBirth, sex, role, donatedAmount, true);

        Person()
        {
            Handles<PersonRegistered>(OnRegistered);
            Handles<Donated>(OnDonated);
            Handles<PhoneNumberUpdated>(OnPhoneNumberUpdated);
        }

        public string Id { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string PhoneNumber { get; private set; }
        public string? DateOfBirth { get; private set; }
        public string? Sex { get; private set; }
        public int State { get; private set; } = 0;
        public Role Role { get; set; } = Role.Donee;

        public double DonatedAmount { get; private set; }

        public void Donate(double amount)
        {
            if (amount < 0)
                throw new ArgumentException("Can only donate positive amounts.");

            Raise(new Donated(amount));
        }

        public void UpdatePhoneNumber(string phoneNumber)
        {
            if (PhoneNumber == phoneNumber)
                return;

            if (string.IsNullOrEmpty(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty.", nameof(phoneNumber));

            Raise(new PhoneNumberUpdated(PhoneNumber, phoneNumber));
        }

        void OnRegistered(PersonRegistered e)
            => (Id, FirstName, LastName, PhoneNumber, DateOfBirth, Sex)
            = (e.Id, e.FirstName, e.LastName, e.PhoneNumber, e.DateOfBirth, e.Sex);

        void OnDonated(Donated e) => DonatedAmount += e.Amount;

        void OnPhoneNumberUpdated(PhoneNumberUpdated e) => PhoneNumber = e.NewNumber;
    }
}

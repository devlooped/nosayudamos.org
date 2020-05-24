#pragma warning disable CS8618 // Non-nullable field is uninitialized. The pattern is intentional for an event-sourced domain object.
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
            Role role = Role.Donee,
            DateTime? dateOfBirth = default,
            Sex? sex = default)
            : this()
        {
            IsReadOnly = false;
            // TODO: validate args.
            Raise(new PersonRegistered(id, firstName, lastName, phoneNumber, role, dateOfBirth, sex));
        }

        Person()
        {
            Handles<PersonRegistered>(OnRegistered);
            Handles<Donated>(OnDonated);
            Handles<PhoneNumberUpdated>(OnPhoneNumberUpdated);
            Handles<TaxStatusAccepted>(OnTaxStatusAccepted);
            Handles<TaxStatusRejected>(OnTaxStatusRejected);
        }

        // NOTE: the [JsonProperty] attributes allow the deserialization from 
        // JSON to be able to set the properties when loading from the last  
        // saved known snapshot state.

        [JsonProperty]
        public string Id { get; private set; }
        
        [JsonProperty]
        public string FirstName { get; private set; }
        
        [JsonProperty]
        public string LastName { get; private set; }
        
        [JsonProperty]
        public string PhoneNumber { get; private set; }
        
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public Role Role { get; set; } = Role.Donee;
        
        [JsonProperty]
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? DateOfBirth { get; private set; }

        [JsonIgnore]
        public int? Age => (DateTime.Now - DateOfBirth)?.Days / 365;

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public Sex? Sex { get; private set; }
        
        [JsonProperty]
        public int State { get; private set; } = 0;

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public TaxStatus TaxStatus { get; private set; } = TaxStatus.Unknown;

        [JsonProperty]
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

        public bool CanUpdateTaxStatus(TaxId taxId)
            => taxId != TaxId.Unknown &&
            (taxId.Kind == TaxIdKind.CUIL ||
            taxId.HasIncomeTax == true ||
            (taxId.Category != TaxCategory.Unknown && taxId.Category != TaxCategory.A));

        /// <summary>
        /// Tries to validate the tax status given the tax information.
        /// Returns whether the information was sufficent to determine 
        /// the final status.
        /// </summary>
        public void UpdateTaxStatus(TaxId taxId)
        {
            if (taxId == TaxId.Unknown)
                return;

            if (taxId == TaxId.None || taxId.Kind == TaxIdKind.CUIL)
            {
                // We just accept CUIL-based registrations, we can't know whether 
                // they pay earnings or not :(
                Raise(new TaxStatusAccepted(taxId));
                return;
            }

            if (taxId.HasIncomeTax == true)
            {
                Raise(new TaxStatusRejected(taxId, TaxStatusRejectedReason.HasIncomeTax));
                return;
            }

            if (taxId.Category == TaxCategory.NotApplicable)
            {
                Raise(new TaxStatusRejected(taxId, TaxStatusRejectedReason.NotApplicable));
                return;
            }

            if (taxId.Category != TaxCategory.Unknown &&
                taxId.Category != TaxCategory.A)
            {
                Raise(new TaxStatusRejected(taxId, TaxStatusRejectedReason.HighCategory));
                return;
            }

            if (taxId.Category == TaxCategory.A)
            {
                Raise(new TaxStatusAccepted(taxId));
                return;
            }

            // Other combinations might not be approved
        }

        void OnRegistered(PersonRegistered e)
            => (Id, FirstName, LastName, PhoneNumber, Role, DateOfBirth, Sex)
            = (e.Id, e.FirstName, e.LastName, e.PhoneNumber, e.Role, e.DateOfBirth, e.Sex);

        void OnDonated(Donated e) => DonatedAmount += e.Amount;

        void OnPhoneNumberUpdated(PhoneNumberUpdated e) => PhoneNumber = e.NewNumber;

        void OnTaxStatusAccepted(TaxStatusAccepted e) => TaxStatus = TaxStatus.Validated;
        
        void OnTaxStatusRejected(TaxStatusRejected e) => TaxStatus = TaxStatus.Rejected;
    }
}

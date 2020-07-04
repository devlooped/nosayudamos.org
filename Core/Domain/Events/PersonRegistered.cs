using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NosAyudamos
{
    public class PersonRegistered : DomainEvent
    {
        public PersonRegistered(
            string personId,
            string firstName,
            string lastName,
            string phoneNumber,
            Role role,
            DateTime? dateOfBirth,
            Sex? sex)
            => (PersonId, FirstName, LastName, PhoneNumber, Role, DateOfBirth, Sex)
            = (personId, firstName, lastName, phoneNumber, role, dateOfBirth, sex);

        public string PersonId { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string PhoneNumber { get; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? DateOfBirth { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Role Role { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Sex? Sex { get; }
    }
}

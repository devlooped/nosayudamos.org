using System;

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
        public DateTime? DateOfBirth { get; }
        public Role Role { get; }
        public Sex? Sex { get; }
    }
}

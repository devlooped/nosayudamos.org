using System;

namespace NosAyudamos
{
    public class PersonRegistered : DomainEvent
    {
        public PersonRegistered(
            string id,
            string firstName,
            string lastName,
            string phoneNumber,
            Role role,
            DateTime? dateOfBirth,
            Sex? sex)
            => (Id, FirstName, LastName, PhoneNumber, Role, DateOfBirth, Sex)
            = (id, firstName, lastName, phoneNumber, role, dateOfBirth, sex);

        public string Id { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string PhoneNumber { get; }
        public DateTime? DateOfBirth { get; }
        public Role Role { get; }
        public Sex? Sex { get; }
    }
}

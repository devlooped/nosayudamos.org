namespace NosAyudamos
{
    public class PersonRegistered : DomainEvent
    {
        public PersonRegistered(
            string id,
            string firstName,
            string lastName,
            string phoneNumber,
            string? dateOfBirth,
            string? sex)
            => (Id, FirstName, LastName, PhoneNumber, DateOfBirth, Sex)
            = (id, firstName, lastName, phoneNumber, dateOfBirth, sex);

        public string Id { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string PhoneNumber { get; }
        public string? DateOfBirth { get; }
        public string? Sex { get; }
    }
}

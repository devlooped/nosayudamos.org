namespace NosAyudamos
{
    class PersonRegistered : DomainEvent
    {
        public PersonRegistered(
            string firstName,
            string lastName,
            string nationalId,
            string phoneNumber,
            string? dateOfBirth,
            string? sex)
            => (FirstName, LastName, NationalId, PhoneNumber, DateOfBirth, Sex)
            = (firstName, lastName, nationalId, phoneNumber, dateOfBirth, sex);

        public string FirstName { get; }
        public string LastName { get; }
        public string NationalId { get; }
        public string PhoneNumber { get; }
        public string? DateOfBirth { get; }
        public string? Sex { get; }
    }
}

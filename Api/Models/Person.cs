namespace NosAyudamos
{
    class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NationalId { get; set; }
        public string DateOfBirth { get; set; }
        public string Sex { get; set; }

        public Person(string firstName, string lastName, string nationalId, string dateOfBirth, string sex) =>
            (FirstName, LastName, NationalId, DateOfBirth, Sex) = (firstName, lastName, nationalId, dateOfBirth, sex);
    }
}

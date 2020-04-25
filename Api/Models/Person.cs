namespace NosAyudamos
{
    public class Person
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string NationalId { get; set; } = "";
        public string DateOfBirth { get; set; } = "";
        public string Sex { get; set; } = "";
        public string State { get; set; } = "";
        public string PhoneNumber { get; set; } = "";

        public Person() { }

        public Person(string firstName, string lastName, string nationalId, string dateOfBirth, string sex, string phoneNumber = "", string state = "0") =>
            (FirstName, LastName, NationalId, DateOfBirth, Sex, PhoneNumber, State) = (firstName, lastName, nationalId, dateOfBirth, sex, phoneNumber, state);
    }
}

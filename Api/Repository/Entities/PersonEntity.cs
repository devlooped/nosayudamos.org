using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class PersonEntity : TableEntity
    {
        public PersonEntity() => RowKey = typeof(PersonEntity).FullName;

        public PersonEntity(string nationalId, string firstName, string lastName, string dateOfBirth, string sex, string phoneNumber, string state = "0")
        {
            PartitionKey = nationalId;
            RowKey = typeof(PersonEntity).FullName;
            FirstName = firstName;
            LastName = lastName;
            DateOfBirth = dateOfBirth;
            Sex = sex;
            PhoneNumber = phoneNumber;
            State = state;
        }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DateOfBirth { get; set; } = "";
        public string Sex { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string State { get; set; } = "";
    }

    class PhoneIdMapEntity : TableEntity
    {
        public PhoneIdMapEntity() { }

        public PhoneIdMapEntity(string phoneNumber, string nationalId)
        {
            PartitionKey = phoneNumber;
            RowKey = typeof(PersonEntity).FullName;
            NationalId = nationalId;
        }

        public string NationalId { get; set; } = "";
    }
}

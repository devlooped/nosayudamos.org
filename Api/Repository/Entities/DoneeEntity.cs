using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    [Table("donee")]
    class DoneeEntity : TableEntity
    {
        public DoneeEntity() { }

        public DoneeEntity(string nationalId, string firstName, string lastName, string dateOfBirth, string sex, string state = "0")
        {
            PartitionKey = nationalId;
            RowKey = typeof(DoneeEntity).FullName;
            FirstName = firstName;
            LastName = lastName;
            DateOfBirth = dateOfBirth;
            Sex = sex;
            State = state;
        }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DateOfBirth { get; set; } = "";
        public string Sex { get; set; } = "";
        public string State { get; set; } = "";
    }
}

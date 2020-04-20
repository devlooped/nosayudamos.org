using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    [Table("donor")]
    class DonorEntity : TableEntity
    {
        public DonorEntity() { }

        public DonorEntity(string nationalId, string firstName, string lastName, string dateOfBirth)
        {
            PartitionKey = nationalId;
            RowKey = typeof(DonorEntity).FullName;
            FirstName = firstName;
            LastName = lastName;
            DateOfBirth = dateOfBirth;
        }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DateOfBirth { get; set; } = "";
    }
}

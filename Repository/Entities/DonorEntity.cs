using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    public class DonorEntity : TableEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DateOfBirth { get; set; }

        public DonorEntity(string nationalId, string firstName, string lastName, string dateOfBirth)
        {
            this.PartitionKey = "find_proper_one"; //TODO: fix
            this.RowKey = nationalId;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.DateOfBirth = dateOfBirth;
        }
    }
}

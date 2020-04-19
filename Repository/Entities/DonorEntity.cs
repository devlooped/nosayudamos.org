using System.Globalization;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    [Table("donor")]
    class DonorEntity : TableEntity
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DateOfBirth { get; set; } = "";

        public DonorEntity() { }

        public DonorEntity(int nationalId, string firstName, string lastName, string dateOfBirth)
        {
            this.PartitionKey = Base62.Encode(nationalId).Substring(0, 2);
            this.RowKey = nationalId.ToString(CultureInfo.InvariantCulture);
            this.FirstName = firstName;
            this.LastName = lastName;
            this.DateOfBirth = dateOfBirth;
        }
    }
}

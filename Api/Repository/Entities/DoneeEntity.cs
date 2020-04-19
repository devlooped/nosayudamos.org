using System;
using System.Globalization;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    [Table("donee")]
    class DoneeEntity : TableEntity
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DateOfBirth { get; set; } = "";
        public string Sex { get; set; } = "";
        public string State { get; set; } = "";

        public DoneeEntity() { }

        public DoneeEntity(string nationalId, string firstName, string lastName, string dateOfBirth, string sex, string state = "0")
        {
            this.PartitionKey = Base62.Encode(Int32.Parse(nationalId, CultureInfo.InvariantCulture)).Substring(0, 2);
            this.RowKey = nationalId;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.DateOfBirth = dateOfBirth;
            this.Sex = sex;
            this.State = state;
        }
    }
}

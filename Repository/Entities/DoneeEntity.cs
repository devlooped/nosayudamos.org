using System.Globalization;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    public class DoneeEntity : TableEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DateOfBirth { get; set; }
        public int State { get; set; }

        public DoneeEntity(int nationalId, string firstName, string lastName, string dateOfBirth, int state)
        {
            this.PartitionKey = Base62.Encode(nationalId).Substring(0, 2);
            this.RowKey = nationalId.ToString(CultureInfo.InvariantCulture);
            this.FirstName = firstName;
            this.LastName = lastName;
            this.DateOfBirth = dateOfBirth;
            this.State = state;
            //TODO: fix
        }
    }
}

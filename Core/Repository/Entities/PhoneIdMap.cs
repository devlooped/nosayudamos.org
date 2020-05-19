using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class PhoneIdMap : TableEntity
    {
        public PhoneIdMap() { }

        public PhoneIdMap(string phoneNumber, string nationalId)
        {
            PartitionKey = phoneNumber;
            RowKey = typeof(PhoneIdMap).FullName;
            NationalId = nationalId;
        }

        public string NationalId { get; set; } = "";
    }
}

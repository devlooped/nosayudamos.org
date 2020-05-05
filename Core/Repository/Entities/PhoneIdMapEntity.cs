using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class PhoneIdMapEntity : TableEntity
    {
        public PhoneIdMapEntity() { }

        public PhoneIdMapEntity(string phoneNumber, string nationalId)
        {
            PartitionKey = phoneNumber;
            RowKey = typeof(PhoneIdMapEntity).FullName;
            NationalId = nationalId;
        }

        public string NationalId { get; set; } = "";
    }
}

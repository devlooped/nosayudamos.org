using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class PhoneIdMap : TableEntity
    {
        public PhoneIdMap() { }

        public PhoneIdMap(string phoneNumber, string nationalId, Role role)
        {
            PartitionKey = phoneNumber;
            RowKey = typeof(PhoneIdMap).FullName;
            NationalId = nationalId;
            Role = role;
        }

        public string NationalId { get; set; } = "";

        public Role Role { get; set; } = Role.Donee;
    }
}

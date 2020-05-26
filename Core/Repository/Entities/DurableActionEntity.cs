using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class DurableActionEntity : TableEntity
    {
        public string Id => PartitionKey;

        public string Action => RowKey;

        public int Attempts { get; set; } = 1;

        public DurableActionEntity() { }

        public DurableActionEntity(string id, string action, int attempts = 0)
        {
            PartitionKey = id;
            RowKey = action;
            Attempts = attempts;
        }
    }
}

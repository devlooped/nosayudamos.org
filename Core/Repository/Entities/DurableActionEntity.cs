using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class DurableActionEntity : TableEntity
    {
        public string Action => PartitionKey;

        public string Id => RowKey;

        public int Attempts { get; set; } = 1;

        public DurableActionEntity() { }

        public DurableActionEntity(string action, string id, int attempts = 0)
        {
            PartitionKey = action;
            RowKey = id;
            Attempts = attempts;
        }
    }
}

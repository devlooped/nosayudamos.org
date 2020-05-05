using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    [Table("actionretry")]
    class ActionRetryEntity : TableEntity
    {
        public string Id => PartitionKey;

        public string Action => RowKey;

        public int RetryCount { get; set; } = 1;

        public ActionRetryEntity() { }

        public ActionRetryEntity(string id, string action, int retryCount = 0)
        {
            PartitionKey = id;
            RowKey = action;
            RetryCount = retryCount;
        }
    }
}

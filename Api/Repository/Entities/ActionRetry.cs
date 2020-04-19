using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    [Table("actionretry")]
    class ActionRetryEntity : TableEntity
    {
        public string Id => base.PartitionKey;

        public string Action => base.RowKey;

        public int RetryCount { get; set; } = 1;

        public ActionRetryEntity() { }

        public ActionRetryEntity(string id, string action, int retryCount = 1)
        {
            this.PartitionKey = id;
            this.RowKey = action;
            this.RetryCount = retryCount;
        }
    }

    enum Action
    {
        RecognizeId
    }
}

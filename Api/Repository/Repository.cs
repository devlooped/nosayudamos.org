using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class Repository<T> : IRepository<T>
        where T : class, ITableEntity
    {
        readonly CloudStorageAccount storageAccount;
        readonly string tableName;
        CloudTable? table;

        public Repository(CloudStorageAccount storageAccount, string tableName)
            => (this.storageAccount, this.tableName)
            = (storageAccount, tableName);

        public async Task<T> PutAsync(T entity)
        {
            var insertOrMergeOperation = TableOperation.InsertOrReplace(entity);

            var table = await GetTableAsync();

            var result = await table.ExecuteAsync(insertOrMergeOperation).ConfigureAwait(false);

            return (T)result.Result;
        }

        public async Task DeleteAsync(T entity)
        {
            var deleteOperation = TableOperation.Delete(entity);
            var table = await GetTableAsync();

            await table.ExecuteAsync(deleteOperation).ConfigureAwait(false);
        }

        public async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);

            var table = await GetTableAsync().ConfigureAwait(false);
            var result = await table.ExecuteAsync(retrieveOperation).ConfigureAwait(false);

            return (T)result.Result;
        }

        async Task<CloudTable> GetTableAsync()
        {
            if (table == null)
            {
                var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                var table = tableClient.GetTableReference(tableName);

                await table.CreateIfNotExistsAsync();

                this.table = table;
            }

            return table;
        }
    }

    interface IRepository<T>
        where T : class, ITableEntity
    {
        Task<T> PutAsync(T entity);

        Task DeleteAsync(T entity);

        Task<T> GetAsync(string partitionKey, string rowKey);
    }
}

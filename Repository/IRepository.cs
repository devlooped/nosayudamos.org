using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;

namespace NosAyudamos
{
    public interface IRepository<T>
        where T : class, ITableEntity
    {
        Task<T> AddOrUpdateAsync(T entity);

        Task DeleteAsync(T entity);

        Task<T> GetAsync(string partitionKey, string rowKey);
    }

    public class Repository<T> : IRepository<T>
        where T : class, ITableEntity, new()
    {
        private readonly string connectionString;
        private readonly string tableName;
        private CloudTable? table;

        public Repository(string connectionString, string tableName)
        {
            this.connectionString = connectionString;
            this.tableName = tableName;
        }

        public async Task<T> AddOrUpdateAsync(T entity)
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

            var table = await GetTableAsync();
            var result = await table.ExecuteAsync(retrieveOperation).ConfigureAwait(false);

            return (T)result.Result;
        }

        private static CloudStorageAccount CreateCloudStorageAccount(string connectionString) => CloudStorageAccount.Parse(connectionString);

        private async Task<CloudTable> GetTableAsync()
        {
            if (this.table == null)
            {
                var storageAccount = CreateCloudStorageAccount(connectionString);
                var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                var table = tableClient.GetTableReference(tableName);

                await table.CreateIfNotExistsAsync();

                this.table = table;
            }

            return this.table;
        }
    }
}

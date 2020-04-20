using System.Reflection;
using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace NosAyudamos
{
    interface IRepositoryFactory
    {
        IRepository<T> Create<T>() where T : class, ITableEntity;
    }

    class RepositoryFactory : IRepositoryFactory
    {
        readonly IEnvironment environment;

        public RepositoryFactory(IEnvironment enviroment) => this.environment = enviroment;

        public IRepository<T> Create<T>()
            where T : class, ITableEntity
        {
            var table = typeof(T).GetTypeInfo().GetCustomAttribute<TableAttribute>();

            Contract.Assert(table != null);

            return new Repository<T>(
                this.environment.GetVariable("StorageConnectionString"), table?.Name!);
        }
    }

    interface IRepository<T>
        where T : class, ITableEntity
    {
        Task<T> PutAsync(T entity);

        Task DeleteAsync(T entity);

        Task<T> GetAsync(string partitionKey, string rowKey);
    }

    class Repository<T> : IRepository<T>
        where T : class, ITableEntity
    {
        readonly string connectionString;
        readonly string tableName;
        CloudTable? table;

        public Repository(string connectionString, string tableName)
        {
            this.connectionString = connectionString;
            this.tableName = tableName;
        }

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

        static CloudStorageAccount CreateCloudStorageAccount(string connectionString) => CloudStorageAccount.Parse(connectionString);

        async Task<CloudTable> GetTableAsync()
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

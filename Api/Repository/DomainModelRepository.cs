using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    abstract class DomainModelRepository
    {
        Lazy<CloudStorageAccount> storageAccount;

        public DomainModelRepository(string connectionString)
            => storageAccount = new Lazy<CloudStorageAccount>(() => CloudStorageAccount.Parse(connectionString));

        public DomainModelRepository(CloudStorageAccount storageAccount)
            => this.storageAccount = new Lazy<CloudStorageAccount>(() => storageAccount);

        protected CloudStorageAccount StorageAccount => storageAccount.Value;

        protected async Task<CloudTable> GetTableAsync(string tableName)
        {
            var tableClient = StorageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();

            return table;
        }
    }
}

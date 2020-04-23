using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    abstract class DomainModelRepository
    {
        public DomainModelRepository(string connectionString) => StorageAccount = CreateCloudStorageAccount(connectionString);

        protected CloudStorageAccount StorageAccount { get; }

        private static CloudStorageAccount CreateCloudStorageAccount(string connectionString)
        {
            return CloudStorageAccount.Parse(connectionString);
        }

        protected async Task<CloudTable> GetTableAsync(string tableName)
        {
            var tableClient = StorageAccount.CreateCloudTableClient(new TableClientConfiguration());
            var table = tableClient.GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();

            return table;
        }
    }
}

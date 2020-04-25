using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    abstract class DomainModelRepository
    {
        public DomainModelRepository(string connectionString) => StorageAccount = CloudStorageAccount.Parse(connectionString);

        protected CloudStorageAccount StorageAccount { get; }

        protected async Task<CloudTable> GetTableAsync(string tableName)
        {
            var tableClient = StorageAccount.CreateCloudTableClient(new TableClientConfiguration());
            var table = tableClient.GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();

            return table;
        }
    }
}

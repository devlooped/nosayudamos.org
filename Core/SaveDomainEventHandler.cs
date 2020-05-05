using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.Threading;

namespace NosAyudamos
{
    class SaveDomainEventHandler : IEventHandler<DomainEvent>
    {
        readonly CloudStorageAccount storageAccount;
        readonly ISerializer serializer;
        readonly AsyncLazy<CloudTable> table;

        public SaveDomainEventHandler(CloudStorageAccount storageAccount, ISerializer serializer)
        {
            this.storageAccount = storageAccount;
            this.serializer = serializer;

            table = new AsyncLazy<CloudTable>(() => GetTableAsync());
        }

        public async Task HandleAsync(DomainEvent e)
        {
            var table = await this.table.GetValueAsync();
            await table.ExecuteAsync(TableOperation.Insert(e.ToEntity(serializer)));
        }

        async Task<CloudTable> GetTableAsync()
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("Event");

            await table.CreateIfNotExistsAsync();
            return table;
        }
    }
}

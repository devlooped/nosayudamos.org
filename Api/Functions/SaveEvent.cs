using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.VisualStudio.Threading;

namespace NosAyudamos.Functions
{
    class SaveEvent : IEventHandler<DomainEvent>
    {
        readonly CloudStorageAccount storageAccount;
        readonly ISerializer serializer;

        AsyncLazy<CloudTable> table;

        public SaveEvent(CloudStorageAccount storageAccount, ISerializer serializer)
        {
            this.storageAccount = storageAccount;
            this.serializer = serializer;

            table = new AsyncLazy<CloudTable>(() => GetTableAsync());
        }

        [FunctionName("save-event")]
        public async Task SaveAsync([EventGridTrigger] EventGridEvent e)
        {
            var table = await this.table.GetValueAsync();
            await table.ExecuteAsync(TableOperation.Insert(e.ToEntity()));
        }

        // NOTE: in this particular case, we go the inverse way, since the persistence is generic for *any* 
        // grid event, not just domain events. This way we can reuse the single storage mechanism but still 
        // force those saves when running integration tests locally for exploring the resulting data.
        public Task HandleAsync(DomainEvent e) => SaveAsync(e.ToEventGrid(serializer));

        async Task<CloudTable> GetTableAsync()
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("Event");

            await table.CreateIfNotExistsAsync();
            return table;
        }
    }
}

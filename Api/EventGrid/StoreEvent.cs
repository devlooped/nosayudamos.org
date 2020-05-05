using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.VisualStudio.Threading;

namespace NosAyudamos.Functions
{
    class StoreEvent
    {
        readonly CloudStorageAccount storageAccount;
        readonly ISerializer serializer;
        readonly SaveDomainEventHandler handler;
        readonly AsyncLazy<CloudTable> table;

        public StoreEvent(CloudStorageAccount storageAccount, ISerializer serializer, SaveDomainEventHandler handler)
        {
            this.storageAccount = storageAccount;
            this.serializer = serializer;
            this.handler = handler;

            table = new AsyncLazy<CloudTable>(() => GetTableAsync());
        }

        [FunctionName("store-event")]
        public async Task SaveAsync([EventGridTrigger] EventGridEvent e)
        {
            var type = Type.GetType(e.EventType);
            if (type != null && typeof(DomainEvent).IsAssignableFrom(type))
            {
                await handler.HandleAsync((DomainEvent)e.GetData(serializer)!);
            }
            else
            {
                var table = await this.table.GetValueAsync();
                await table.ExecuteAsync(TableOperation.Insert(e.ToEntity()));
            }
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

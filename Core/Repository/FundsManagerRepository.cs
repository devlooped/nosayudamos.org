using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Streamstone;

namespace NosAyudamos
{
    /// <summary>
    /// Repository for the <see cref="FundsManager"/>.
    /// </summary>
    interface IFundsManagerRepository
    {
        /// <summary>
        /// Inserts or updates the funds manager.
        /// </summary>
        Task<FundsManager> PutAsync(FundsManager funds);
        /// <summary>
        /// Retrieves or creates the funds manager..
        /// </summary>
        /// <param name="readOnly">If <see langword="true"/>, will only return the last-known state for the funds manager, 
        /// rather than loading its history too, and no mutation operations will be allowed on it.</param>
        Task<FundsManager> GetAsync(bool readOnly = true);
    }

    class FundsManagerRepository : IFundsManagerRepository
    {
        const string FundsPartitionKey = "42";

        readonly ISerializer serializer;
        readonly CloudStorageAccount storageAccount;
        readonly CloudTable? cloudTable = null;

        public FundsManagerRepository(ISerializer serializer, CloudStorageAccount storageAccount)
            => (this.serializer, this.storageAccount)
            = (serializer, storageAccount);

        public async Task<FundsManager> PutAsync(FundsManager funds)
        {
            var table = await GetTableAsync();

            var partition = new Partition(table, FundsPartitionKey);
            var result = await Stream.TryOpenAsync(partition);
            var stream = result.Found ? result.Stream : new Stream(partition);
            var header = DataEntity.Create(FundsPartitionKey, funds, serializer);
            header.Version = stream.Version + funds.Events.Count();

            await Stream.WriteAsync(partition, funds.Version, funds.Events.Select((e, i) =>
                e.ToEventData(stream.Version + i, header)).ToArray());

            funds.AcceptEvents();

            return funds;
        }

        public async Task<FundsManager> GetAsync(bool readOnly = true)
        {
            if (readOnly)
            {
                var header = await GetAsync<DataEntity>(FundsPartitionKey, typeof(FundsManager).FullName!).ConfigureAwait(false);
                if (header == null)
                    return FundsManager.Create();

                if (header.Data == null)
                    throw new ArgumentException(Strings.DomainRepository.EmptyData);

                return serializer.Deserialize<FundsManager>(header.Data);
            }

            var table = await GetTableAsync();
            var partition = new Partition(table, FundsPartitionKey);
            var existent = await Stream.TryOpenAsync(partition);
            if (!existent.Found)
                return FundsManager.Create();

            var events = (await Stream.ReadAsync<DomainEventEntity>(partition))
                .Events.Select(e => e.ToDomainEvent(serializer)).ToList();

            return new FundsManager(events) { Version = existent.Stream.Version };
        }

        async Task<T> GetAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var table = await GetTableAsync();

            var result = await table.ExecuteAsync(
                TableOperation.Retrieve<T>(partitionKey, rowKey)).ConfigureAwait(false);

            return (T)result.Result;
        }

        async Task<CloudTable> GetTableAsync()
            => cloudTable ?? await GetTableAsync("Funds");

        async Task<CloudTable> GetTableAsync(string tableName)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();

            return table;
        }
    }
}

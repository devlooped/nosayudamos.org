using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Streamstone;

namespace NosAyudamos
{
    /// <summary>
    /// Repository of registered people.
    /// </summary>
    interface IPersonRepository
    {
        /// <summary>
        /// Inserts or updates the given person information.
        /// </summary>
        Task<Person> PutAsync(Person person);
        /// <summary>
        /// Retrieves an existing person from its <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The national identifier for the person.</param>
        /// <param name="readOnly">If <see langword="true"/>, will only return the last-known state for the person, 
        /// rather than loading its history too, and no mutation operations will be allowed on it.</param>
        /// <returns>The stored person information or <see langword="null"/> if none was found with the given <paramref name="id"/>.</returns>
        Task<Person?> GetAsync(string id, bool readOnly = true);
        /// <summary>
        /// Tries to locate the person that matches the given phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to attempt to map to a person.</param>
        /// <param name="readOnly">If <see langword="true"/>, will only return the last-known state for the person, 
        /// rather than loading its history too, and no mutation operations will be allowed on it.</param>
        /// <returns>The stored person or <see langword="null"/> if none was found with the given <paramref name="phoneNumber"/>.</returns>
        Task<Person?> FindAsync(string phoneNumber, bool readOnly = true);
    }

    class PersonRepository : IPersonRepository
    {
        readonly ISerializer serializer;
        readonly CloudStorageAccount storageAccount;
        readonly CloudTable? cloudTable = null;

        public PersonRepository(ISerializer serializer, CloudStorageAccount storageAccount)
            => (this.serializer, this.storageAccount)
            = (serializer, storageAccount);

        public async Task<Person> PutAsync(Person person)
        {
            var table = await GetTableAsync();
            var existing = await GetAsync(person.Id, readOnly: true).ConfigureAwait(false);

            // First check if the person changed phone numbers since our last interaction
            if (existing != null && existing.PhoneNumber != person.PhoneNumber)
            {
                var mapEntity =
                    await GetAsync<PhoneIdMap>(existing.PhoneNumber).ConfigureAwait(false);

                if (mapEntity != null)
                {
                    await table.ExecuteAsync(
                        TableOperation.Delete(mapEntity)).ConfigureAwait(false);
                }
            }

            await table.ExecuteAsync(
                TableOperation.InsertOrReplace(
                    new PhoneIdMap(person.PhoneNumber, person.Id!))).ConfigureAwait(false);

            var partition = new Partition(table, person.Id!);
            var result = await Stream.TryOpenAsync(partition);
            var stream = result.Found ? result.Stream : new Stream(partition);
            var header = DataEntity.Create(person.Id!, person, serializer);
            header.Version = stream.Version + person.Events.Count();

            await Stream.WriteAsync(partition, person.Version, person.Events.Select((e, i) =>
                e.ToEventData(stream.Version + i, header)).ToArray());

            person.AcceptEvents();

            return person;
        }

        public async Task<Person?> GetAsync(string id, bool readOnly = true)
        {
            if (string.IsNullOrEmpty(id))
                return default;

            if (readOnly)
            {
                var header = await GetAsync<DataEntity>(id, typeof(Person).FullName!).ConfigureAwait(false);
                if (header == null)
                    return default;

                if (header.Data == null)
                    throw new ArgumentException(Strings.DomainRepository.EmptyData);

                return serializer.Deserialize<Person>(header.Data);
            }

            var table = await GetTableAsync();
            var partition = new Partition(table, id);
            var existent = await Stream.TryOpenAsync(partition);
            if (!existent.Found)
                return default;

            var events = (await Stream.ReadAsync<DomainEventEntity>(partition))
                .Events.Select(e => e.ToDomainEvent(serializer)).ToList();

            return new Person(events) { Version = existent.Stream.Version };
        }

        public async Task<Person?> FindAsync(string phoneNumber, bool readOnly = true)
        {
            var mapEntity = await GetAsync<PhoneIdMap>(phoneNumber).ConfigureAwait(false);

            return mapEntity == null ? default :
                await GetAsync(mapEntity.NationalId, readOnly).ConfigureAwait(false);
        }

        Task<T> GetAsync<T>(string partitionKey) where T : class, ITableEntity, new()
            => GetAsync<T>(partitionKey, typeof(T).FullName!);

        async Task<T> GetAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var table = await GetTableAsync();

            var result = await table.ExecuteAsync(
                TableOperation.Retrieve<T>(partitionKey, rowKey)).ConfigureAwait(false);

            return (T)result.Result;
        }

        async Task<CloudTable> GetTableAsync()
            => cloudTable ?? await GetTableAsync("Person");

        async Task<CloudTable> GetTableAsync(string tableName)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();

            return table;
        }
    }
}

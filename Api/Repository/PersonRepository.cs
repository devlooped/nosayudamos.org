using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using NosAyudamos.Properties;
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
        /// Retrieves an existing person from its <paramref name="nationalId"/>.
        /// </summary>
        /// <param name="nationalId">The national identifier for the person.</param>
        /// <param name="readOnly">If <see langword="true"/>, will only return the last-known state for the person, 
        /// rather than loading its history too, and no mutation operations will be allowed on it.</param>
        /// <returns>The stored person information or <see langword="null"/> if none was found with the given <paramref name="nationalId"/>.</returns>
        Task<Person?> GetAsync(string nationalId, bool readOnly = true);
        /// <summary>
        /// Tries to locate the person that matches the given phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to attempt to map to a person.</param>
        /// <param name="readOnly">If <see langword="true"/>, will only return the last-known state for the person, 
        /// rather than loading its history too, and no mutation operations will be allowed on it.</param>
        /// <returns>The stored person or <see langword="null"/> if none was found with the given <paramref name="phoneNumber"/>.</returns>
        Task<Person?> FindAsync(string phoneNumber, bool readOnly = true);
    }

    class PersonRepository : DomainModelRepository, IPersonRepository
    {
        readonly ISerializer serializer;
        readonly CloudTable? cloudTable = null;

        // Test constructor
        internal PersonRepository(ISerializer serializer, CloudStorageAccount storageAccount)
            : base(storageAccount)
            => (this.serializer) = (serializer);

        public PersonRepository(ISerializer serializer, IEnvironment environment)
            : base(environment.GetVariable("StorageConnectionString"))
            => this.serializer = serializer;

        public async Task<Person> PutAsync(Person person)
        {
            var table = await GetTableAsync();
            var existing = await GetAsync(person.NationalId, readOnly: true).ConfigureAwait(false);

            // First check if the person changed phone numbers since our last interaction
            if (existing != null && existing.PhoneNumber != person.PhoneNumber)
            {
                var mapEntity =
                    await GetAsync<PhoneIdMapEntity>(existing.PhoneNumber).ConfigureAwait(false);

                if (mapEntity != null)
                {
                    await table.ExecuteAsync(
                        TableOperation.Delete(mapEntity)).ConfigureAwait(false);
                }
            }

            await table.ExecuteAsync(
                TableOperation.InsertOrReplace(
                    new PhoneIdMapEntity(person.PhoneNumber, person.NationalId))).ConfigureAwait(false);

            var partition = new Partition(table, person.NationalId);
            var result = await Stream.TryOpenAsync(partition);
            var stream = result.Found ? result.Stream : new Stream(partition);
            var header = EntityData.Create(person.NationalId, person, serializer);
            header.Version = stream.Version + person.Events.Count();

            await Stream.WriteAsync(stream, person.Events.Select((e, i) =>
                ToEventData(e, stream.Version + i, header)).ToArray());

            person.AcceptEvents();

            return person;
        }

        public async Task<Person?> GetAsync(string nationalId, bool readOnly = true)
        {
            if (readOnly)
            {
                var header = await GetAsync<EntityData>(nationalId, typeof(Person).FullName!).ConfigureAwait(false);
                if (header.Data == null)
                    throw new ArgumentException(Strings.PersonRepository.EmptyData);

                return serializer.Deserialize<Person>(header.Data);
            }

            var table = await GetTableAsync();
            var partition = new Partition(table, nationalId);
            var existent = await Stream.TryOpenAsync(partition);
            if (!existent.Found)
                return default;

            var events = (await Stream.ReadAsync<DomainEventEntity>(partition)).Events.Select(ToDomainEvent).ToList();

            return new Person(events);
        }

        public async Task<Person?> FindAsync(string phoneNumber, bool readOnly = true)
        {
            var mapEntity = await GetAsync<PhoneIdMapEntity>(phoneNumber).ConfigureAwait(false);

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

        static EventData ToEventData(DomainEvent e, int version, params ITableEntity[] includes)
        {
            var properties = new
            {
                e.Id,
                EventType = e.GetType().FullName,
                Data = new Serializer().Serialize(e),
                DataVersion = (e.GetType().Assembly.GetName().Version ?? new Version(1, 0)).ToString(2),
                Version = version,
            };

            return new EventData(
                EventId.None,
                EventProperties.From(properties),
                EventIncludes.From(includes.Select(x => Include.InsertOrReplace(x))));
        }

        DomainEvent ToDomainEvent(DomainEventEntity entity)
        {
            if (entity.Data == null)
                throw new ArgumentException(Strings.PersonRepository.EmptyData);
            if (entity.EventType == null)
                throw new ArgumentException(Strings.PersonRepository.EmptyEventType);

            var entityType = Type.GetType(entity.EventType, true)!;
            if (!typeof(DomainEvent).IsAssignableFrom(entityType))
                throw new ArgumentException(Strings.PersonRepository.EmptyData);

            var e = (DomainEvent)serializer.Deserialize(entity.Data, entityType);
            e.Id = (entity.EventId ?? Guid.NewGuid());
            e.Version = entity.Version;
            e.When = entity.Timestamp;
            return e;
        }
    }
}

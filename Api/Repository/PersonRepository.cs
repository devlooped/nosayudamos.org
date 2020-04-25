using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Azure.Cosmos.Table;
using NosAyudamos.Properties;

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
        /// <returns>The stored person information.</returns>
        /// <exception cref="ArgumentException">An existing person with the given <paramref name="nationalId"/> was not found.</exception>
        Task<Person> GetAsync(string nationalId);
        Task<Person?> FindAsync(string phoneNumber);
    }

    class PersonRepository : DomainModelRepository, IPersonRepository
    {
        readonly IEnvironment environment;
        readonly IMapper mapper;
        readonly CloudTable? cloudTable = null;
        readonly string rowKey = typeof(PersonEntity).FullName!;

        public PersonRepository(IMapper mapper, IEnvironment environment) : base(environment.GetVariable("StorageConnectionString"))
        {
            this.mapper = mapper;
            this.environment = environment;
        }

        public async Task<Person> PutAsync(Person person)
        {
            var table = await GetTableAsync();
            var entity = await GetAsync<PersonEntity>(person.NationalId).ConfigureAwait(false);

            if (entity != null)
            {
                // First check if the person changed phone numbers since our last interaction
                if (entity.PhoneNumber != person.PhoneNumber)
                {
                    var mapEntity =
                        await GetAsync<PhoneIdMapEntity>(entity.PhoneNumber).ConfigureAwait(false);

                    if (mapEntity != null)
                    {
                        await table.ExecuteAsync(
                            TableOperation.Delete(mapEntity)).ConfigureAwait(false);
                    }
                }
            }

            await table.ExecuteAsync(
                TableOperation.InsertOrReplace(
                    new PhoneIdMapEntity(person.PhoneNumber, person.NationalId))).ConfigureAwait(false);

            await table.ExecuteAsync(
                TableOperation.InsertOrReplace(mapper.Map<Person, PersonEntity>(person))).ConfigureAwait(false);

            return person;
        }

        public async Task<Person> GetAsync(string nationalId)
        {
            var entity = await GetAsync<PersonEntity>(nationalId).ConfigureAwait(false);

            return mapper.Map<PersonEntity, Person>(entity ??
                throw new ArgumentException(Strings.PersonRepository.NationalIdNotFound(nationalId)));
        }

        public async Task<Person?> FindAsync(string phoneNumber)
        {
            var mapEntity = await GetAsync<PhoneIdMapEntity>(phoneNumber).ConfigureAwait(false);

            if (mapEntity != null)
            {
                var entity =
                    await GetAsync<PersonEntity>(mapEntity.NationalId).ConfigureAwait(false);

                if (entity != null)
                {
                    return mapper.Map<PersonEntity, Person>(entity);
                }
            }

            return null;
        }


        private async Task<T> GetAsync<T>(string partitionKey) where T : class, ITableEntity, new()
        {
            var table = await GetTableAsync();

            var result = await table.ExecuteAsync(
                TableOperation.Retrieve<T>(partitionKey, rowKey)).ConfigureAwait(false);

            return (T)result.Result;
        }

        private async Task<CloudTable> GetTableAsync()
            => cloudTable ?? await GetTableAsync("person");
    }
}

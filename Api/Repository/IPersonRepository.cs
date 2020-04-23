using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
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

        public async Task<Person?> GetAsync(string phoneNumber)
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
        {
            return cloudTable ?? await GetTableAsync("person");
        }
    }

    interface IPersonRepository
    {
        Task<Person> PutAsync(Person person);

        Task<Person?> GetAsync(string phoneNumber);
    }
}

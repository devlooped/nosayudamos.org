using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace NosAyudamos
{
    public class EntityRepositoryTests : IDisposable
    { 
        [Fact]
        public async Task CanSavePersonMessagingMap()
        {
            var expected = new PersonMessagingMap("23696294", "123", "987");

            var repository = await GetRepositoryAsync<PersonMessagingMap>();

            await repository.PutAsync(expected);

            var actual = await repository.GetAsync(expected.PersonId);

            Assert.Equal(expected.PersonId, actual.PersonId);
            Assert.Equal(expected.PhoneNumber, actual.PhoneNumber);
            Assert.Equal(expected.SystemNumber, actual.SystemNumber);
        }

        [Fact]
        public async Task CanGetAll()
        {
            var repository = await GetRepositoryAsync<NestedType>();

            await repository.PutAsync(new NestedType { Id = Constants.Donee.PhoneNumber });
            await repository.PutAsync(new NestedType { Id = Constants.Donor.PhoneNumber });
            await repository.PutAsync(new NestedType { Id = Constants.System.PhoneNumber });

            var count = 0;

            await foreach (var entity in repository.GetAllAsync())
            {
                count++;
            }

            Assert.Equal(3, count);
        }

        [Fact]
        public async Task CanSaveNested()
        {
            var expected = new NestedType
            {
                Id = "123",
                IntProp = 25,
                StringProp = "Foo"
            };

            var repository = await GetRepositoryAsync<NestedType>();

            await repository.PutAsync(expected);

            var actual = await repository.GetAsync(expected.Id);

            Assert.Equal(expected.StringProp, actual.StringProp);

        }

        async Task<EntityRepository<T>> GetRepositoryAsync<T>([CallerMemberName] string? tableName = null)
        {
            var tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);

            await table.DeleteIfExistsAsync();
            tableNames.Add(tableName);
            return new EntityRepository<T>(CloudStorageAccount.DevelopmentStorageAccount, new Serializer(), tableName);
        }

        List<string> tableNames = new List<string>();

        public void Dispose()
        {
            var tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
            foreach (var tableName in tableNames)
            {
                var table = tableClient.GetTableReference(tableName);
                table.DeleteIfExists();
            }
        }

        class NestedType
        {
            [Key]
            public string Id { get; set; }
            public int IntProp { get; set; }
            public string StringProp { get; set; }
        }
    }
}

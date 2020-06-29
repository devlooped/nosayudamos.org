using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace NosAyudamos
{
    public sealed class EntityRepositoryTests : IDisposable
    {
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

        [Fact]
        public async Task WhenSavingTypeWithNoPartitionKey_ThenUsesTypeNameAsDefault()
        {
            var repository = await GetRepositoryAsync<TypeWithNoPartitionKey>();

            await repository.PutAsync(new TypeWithNoPartitionKey { Id = "123", Value = "asdf" });

            var table = GetTable();

            var result = await table.ExecuteAsync(TableOperation.Retrieve(typeof(TypeWithNoPartitionKey).Name, "123"));

            Assert.NotNull(result?.Result);
        }

        class TypeWithNoPartitionKey
        {
            [RowKey]
            public string Id { get; set; }
            public string Value { get; set; }
        }


        async Task<EntityRepository<T>> GetRepositoryAsync<T>([CallerMemberName] string tableName = null) where T : class
        {
            var table = GetTable(tableName);
            await table.DeleteIfExistsAsync();
            tableNames.Add(tableName);
            return new EntityRepository<T>(CloudStorageAccount.DevelopmentStorageAccount, new Serializer(), tableName);
        }

        static CloudTable GetTable([CallerMemberName] string tableName = null)
        {
            var tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            return table;
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
            [RowKey]
            public string Id { get; set; }
            public int IntProp { get; set; }
            public string StringProp { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace NosAyudamos
{
    public static class PopulationExtensions
    {
        public static Task<Population> GetPopulationAsync(this IRepository<Population> repository, string country, string city)
            => repository.GetAsync(country, city);
    }

    public class Population
    {
        public Population(string country, string city, long quantity)
            => (Country, City, Quantity)
            = (country, city, quantity);

        [PartitionKey]
        public string Country { get; set; }
        [RowKey]
        public string City { get; set; }
        public long Quantity { get; set; }
    }

    public sealed class RepositoryTests : IDisposable
    {
        List<string> tableNames = new List<string>();

        [Fact]
        public async Task WhenSaving_ThenCanRetrieve()
        {
            var repository = await GetRepositoryAsync<Population>();
            var expected = new Population("Argentina", "BuenosAires", 10000000);

            await repository.PutAsync(expected);

            var table = GetTable();

            var result = await table.ExecuteAsync(TableOperation.Retrieve(expected.Country, expected.City));

            Assert.NotNull(result?.Result);

            var actual = await repository.GetPopulationAsync(expected.Country, expected.City);

            Assert.Equal(expected.Quantity, actual.Quantity);
        }

        async Task<IRepository<T>> GetRepositoryAsync<T>([CallerMemberName] string tableName = null) where T : class
        {
            tableName = tableName.Replace("_", "");
            var table = GetTable(tableName);
            await table.DeleteIfExistsAsync();
            tableNames.Add(tableName);
            return new Repository<T>(CloudStorageAccount.DevelopmentStorageAccount, new Serializer(), tableName);
        }

        static CloudTable GetTable([CallerMemberName] string tableName = null)
        {
            tableName = tableName.Replace("_", "");
            var tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            return table;
        }

        public void Dispose()
        {
            var tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
            foreach (var tableName in tableNames)
            {
                var table = tableClient.GetTableReference(tableName);
                table.DeleteIfExists();
            }
        }
    }
}

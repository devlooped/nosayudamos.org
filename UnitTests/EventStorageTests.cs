using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace NosAyudamos
{
    public class EventStorageTests
    {
        [Fact]
        public async Task SaveEvents()
        {
            var env = new Environment();
            var serializer = new Serializer();
            var client = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
            var table = client.GetTableReference(nameof(SaveEvents));
            await table.DeleteIfExistsAsync();
            await table.CreateIfNotExistsAsync();

            await table.ExecuteAsync(TableOperation.Insert(
                new MessageReceived(Constants.Donee.PhoneNumber, Constants.System.PhoneNumber, "Hola")
                    .ToEventGrid(serializer).ToEntity()));

            var person = Constants.Donee.Create();
            person.UpdatePhoneNumber(Constants.Donee2.PhoneNumber);

            foreach (var e in person.Events)
            {
                await table.ExecuteAsync(TableOperation.Insert(e.ToEntity(serializer)));
            }
        }
    }
}

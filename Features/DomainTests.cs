using System.Threading.Tasks;
using Autofac;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class DomainTests
    {
        public async Task CanPushRepositoryEventsToGrid()
        {
            var container = new FeatureContainer();

            await CloudStorageAccount.DevelopmentStorageAccount.ClearStorageAsync();

            var repo = container.Resolve<IPersonRepository>();

            await repo.PutAsync(Constants.Donor.Create());

            var person = await repo.GetAsync<Donor>(Constants.Donor.Id, false);

            person.Donate(500);

            await repo.PutAsync(person);
        }
    }
}

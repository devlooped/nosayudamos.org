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

            await repo.PutAsync(new Person("23696294", "Daniel", "Cazzulino", "5491159278282"));

            var person = await repo.GetAsync("23696294", false);

            person.Donate(500);

            await repo.PutAsync(person);
        }
    }
}

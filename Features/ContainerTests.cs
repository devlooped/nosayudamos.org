using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Autofac;

namespace NosAyudamos
{
    public class ContainerTests
    {
        public async Task CanResolveGeneric()
        {
            var container = new FeatureContainer();
            var repo = container.Resolve<IEntityRepository<Foo>>();

            container.Resolve<IRepository<PhoneIdMapEntity>>();

            await repo.PutAsync(new Foo { Id = "Bar", Value = "Baz" });
        }

        public class Foo
        {
            [Key]
            public string Id { get; set; }
            public string Value { get; set; }
        }
    }
}

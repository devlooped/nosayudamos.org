using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Autofac;
using Xunit;
using System;

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

        public void EnvironmentGetSecret()
        {
            var env = new FeatureEnvironment();

            Assert.True(Guid.TryParse(env.GetVariable("LuisAppId"), out _));
        }
    }
}

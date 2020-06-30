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

            container.Resolve<IRepository<Foo>>();

            await repo.PutAsync(new Foo { Id = "Bar", Value = "Baz" });
        }

        public class Foo
        {
            [RowKey]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public void EnvironmentGetSecret()
        {
            var env = new Environment();

            Assert.True(Guid.TryParse(env.GetVariable("LuisAppId"), out _));
        }
    }
}

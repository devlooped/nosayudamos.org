using Autofac;
using NosAyudamos.Repository;

namespace NosAyudamos
{
    class TestEntityRepositoryFactory : IEntityRepositoryFactory
    {
        readonly IContainer container;

        public TestEntityRepositoryFactory(IContainer container) => this.container = container;

        public IEntityRepository<T> Create<T>() where T : class => container.Resolve<IEntityRepository<T>>();
    }
}

using Autofac;

namespace NosAyudamos
{
    class TestRepositoryFactory : IRepositoryFactory
    {
        readonly IContainer container;

        public TestRepositoryFactory(IContainer container) => this.container = container;

        IRepository<T> IRepositoryFactory.Create<T>() => container.Resolve<IRepository<T>>();
    }
}

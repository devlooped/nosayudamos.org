using Autofac;

namespace NosAyudamos
{
    class FeatureRepositoryFactory : IRepositoryFactory
    {
        readonly IContainer container;

        public FeatureRepositoryFactory(IContainer container) => this.container = container;

        IRepository<T> IRepositoryFactory.Create<T>() => container.Resolve<IRepository<T>>();
    }
}

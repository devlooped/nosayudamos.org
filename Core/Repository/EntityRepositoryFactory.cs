using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos.Repository
{
    class EntityRepositoryFactory : IEntityRepositoryFactory
    {
        readonly CloudStorageAccount storageAccount;
        readonly ISerializer serializer;

        public EntityRepositoryFactory(CloudStorageAccount storageAccount, ISerializer serializer)
            => (this.storageAccount, this.serializer)
            = (storageAccount, serializer);

        public IEntityRepository<T> Create<T>() where T : class => new EntityRepository<T>(storageAccount, serializer);
    }

    interface IEntityRepositoryFactory
    {
        IEntityRepository<T> Create<T>() where T : class;
    }
}

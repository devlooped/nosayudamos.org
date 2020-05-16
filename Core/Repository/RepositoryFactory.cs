using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    interface IRepositoryFactory
    {
        IRepository<T> Create<T>() where T : class, ITableEntity;
    }

    class RepositoryFactory : IRepositoryFactory
    {
        readonly CloudStorageAccount storageAccount;

        public RepositoryFactory(CloudStorageAccount storageAccount) => this.storageAccount = storageAccount;

        public IRepository<T> Create<T>() where T : class, ITableEntity => new Repository<T>(storageAccount);
    }
}

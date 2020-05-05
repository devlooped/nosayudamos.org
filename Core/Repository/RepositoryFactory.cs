using System.Reflection;
using Microsoft.Azure.Cosmos.Table;
using System.Diagnostics.Contracts;

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

        public IRepository<T> Create<T>()
            where T : class, ITableEntity
        {
            var table = typeof(T).GetTypeInfo().GetCustomAttribute<TableAttribute>();

            Contract.Assert(table != null);

            return new Repository<T>(storageAccount, table?.Name!);
        }
    }
}

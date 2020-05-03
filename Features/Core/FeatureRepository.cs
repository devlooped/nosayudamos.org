using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class FeatureRepository<T> : IRepository<T>
        where T : class, ITableEntity
    {
        Dictionary<(string, string), T> values = new Dictionary<(string, string), T>();

        public Task DeleteAsync(T entity)
        {
            values.Remove((entity.PartitionKey, entity.RowKey));
            return Task.CompletedTask;
        }

        public Task<T> GetAsync(string partitionKey, string rowKey)
        {
            if (values.TryGetValue((partitionKey, rowKey), out var value))
                return Task.FromResult(value);

            return Task.FromResult(default(T));
        }

        public Task<T> PutAsync(T entity)
        {
            values[(entity.PartitionKey, entity.RowKey)] = entity;
            return Task.FromResult(entity);
        }
    }

    class FeatureRepositoryFactory : IRepositoryFactory
    {
        readonly IContainer container;

        public FeatureRepositoryFactory(IContainer container) => this.container = container;

        IRepository<T> IRepositoryFactory.Create<T>() => container.Resolve<IRepository<T>>();
    }
}

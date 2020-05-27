using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class TestRepository<T> : IRepository<T>
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
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class TestRepository<T> : IRepository<T> where T : class
    {
        static readonly Func<T, string> getPartitionKey = PartitionKeyAttribute.CreateAccessor<T>();
        static readonly Func<T, string> getRowKey = RowKeyAttribute.CreateAccessor<T>();

        Dictionary<(string, string), T> values = new Dictionary<(string, string), T>();

        public Task DeleteAsync(T entity)
        {
            values.Remove((getPartitionKey(entity), getRowKey(entity)));
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string partitionKey, string rowKey)
        {
            values.Remove((partitionKey, rowKey));
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
            values[(getPartitionKey(entity), getRowKey(entity))] = entity;
            return Task.FromResult(entity);
        }
    }
}

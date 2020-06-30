using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class TestEntityRepository<T> : IEntityRepository<T> where T : class
    {
        ConcurrentDictionary<string, T> values = new ConcurrentDictionary<string, T>();

        public Task DeleteAsync(string rowKey)
        {
            values.TryRemove(rowKey, out _);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity)
        {
            values.TryRemove(GetRowKey(entity), out _);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<T> GetAllAsync()
        {
            await Task.CompletedTask;
            foreach (var item in values.Values)
            {
                yield return item;
            }
        }

        public Task<T> GetAsync(string rowKey)
        {
            if (values.TryGetValue(rowKey, out var value))
                return Task.FromResult(value);

            return Task.FromResult<T>(default);
        }

        public Task<T> PutAsync(T entity)
        {
            values[GetRowKey(entity)] = entity;
            return Task.FromResult(entity);
        }

        static string GetRowKey(T entity)
        {
            var rowKeyProp = typeof(T).GetProperties()
                .FirstOrDefault(prop => prop.GetCustomAttribute<RowKeyAttribute>() != null)
                ?? throw new ArgumentException("Entity must have one property annotated with [RowKey]");

            if (rowKeyProp.PropertyType != typeof(string))
                throw new ArgumentException("Property annotated with [RowKey] must be of type string.");

            var rowKey = (string)rowKeyProp.GetValue(entity);
            if (string.IsNullOrEmpty(rowKey))
                throw new ArgumentException($"RowKey property {rowKeyProp.Name} cannot be null or empty.");

            return rowKey;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using NosAyudamos.Repository;

namespace NosAyudamos.Core
{
    class FeatureEntityRepository<T> : IEntityRepository<T>
    {
        ConcurrentDictionary<string, T> values = new ConcurrentDictionary<string, T>();
        
        public Task DeleteAsync(T entity)
        {
            values.TryRemove(GetKey(entity), out _);
            return Task.CompletedTask;
        }

        public Task<T> GetAsync(string key)
        {
            if (values.TryGetValue(key, out var value))
                return Task.FromResult(value);

            return Task.FromResult<T>(default);
        }

        public Task<T> PutAsync(T entity)
        {
            values[GetKey(entity)] = entity;
            return Task.FromResult(entity);
        }

        static string GetKey(T entity)
        {
            // TODO: maybe we can support composite keys by allowing more than 
            // one [Key]-annotated property?

            var idProp = typeof(T).GetProperties()
                .FirstOrDefault(prop => prop.GetCustomAttribute<KeyAttribute>() != null)
                ?? throw new ArgumentException("Entity must have one property annotated with [Key]");

            if (idProp.PropertyType != typeof(string))
                throw new ArgumentException("Property annotated with [Key] must be of type string.");

            var id = (string)idProp.GetValue(entity);
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException($"Key property {idProp.Name} cannot be null or empty.");

            return id;
        }
    }

    class FeatureEntityRepositoryFactory : IEntityRepositoryFactory
    {
        readonly IContainer container;

        public FeatureEntityRepositoryFactory(IContainer container) => this.container = container;

        public IEntityRepository<T> Create<T>() => container.Resolve<IEntityRepository<T>>();
    }
}

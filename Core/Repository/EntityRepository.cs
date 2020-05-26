using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;

namespace NosAyudamos
{
    class EntityRepository<T> : IEntityRepository<T> where T : class
    {
        readonly CloudStorageAccount storageAccount;
        readonly ISerializer serializer;
        readonly AsyncLazy<CloudTable> table;

        public EntityRepository(CloudStorageAccount storageAccount, ISerializer serializer)
            : this(storageAccount, serializer, "Entity") { }

        public EntityRepository(CloudStorageAccount storageAccount, ISerializer serializer, string tableName)
        {
            this.storageAccount = storageAccount;
            this.serializer = serializer;
            table = new AsyncLazy<CloudTable>(() => GetTableAsync(tableName ?? "Entity"));
        }

        public async Task DeleteAsync(T entity)
        {
            var key = GetKey(entity);
            var table = await this.table.GetValueAsync().ConfigureAwait(false);

            await table.ExecuteAsync(TableOperation.Delete(
                new DynamicTableEntity(typeof(T).FullName!, key) { ETag = "*" }))
                .ConfigureAwait(false);
        }

        public async IAsyncEnumerable<T> GetAllAsync()
        {
            var table = await this.table.GetValueAsync().ConfigureAwait(false);
            var query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, typeof(T).FullName!));

            TableQuerySegment<DynamicTableEntity>? querySegment = null;
            while (querySegment == null || querySegment.ContinuationToken != null)
            {
                querySegment = await table.ExecuteQuerySegmentedAsync(query, querySegment != null ? querySegment.ContinuationToken : null);
                foreach (var entity in querySegment)
                {
                    yield return ToEntity(entity);
                }
            }
        }

        public async Task<T?> GetAsync(string key)
        {
            var table = await this.table.GetValueAsync().ConfigureAwait(false);
            var result = await table.ExecuteAsync(TableOperation.Retrieve(typeof(T).FullName!, key))
                .ConfigureAwait(false);

            if (result?.Result == null)
                return default;

            return ToEntity((DynamicTableEntity)result.Result);
        }

        public async Task<T> PutAsync(T entity)
        {
            var key = GetKey(entity);
            var properties = (entity ?? throw new ArgumentNullException(nameof(entity))).GetType()
                .GetProperties()
                .Where(prop => prop.GetCustomAttribute<KeyAttribute>() == null)
                .ToDictionary(prop => prop.Name, prop => EntityProperty.CreateEntityPropertyFromObject(prop.GetValue(entity)));

            var table = await this.table.GetValueAsync().ConfigureAwait(false);

            var result = await table.ExecuteAsync(TableOperation.InsertOrReplace(
                new DynamicTableEntity(typeof(T).FullName!, key, "*", properties)))
                .ConfigureAwait(false);

            return ToEntity((DynamicTableEntity)result.Result);
        }

        T ToEntity(DynamicTableEntity entity)
        {
            using var json = new StringWriter();
            using var writer = new JsonTextWriter(json);

            // Write entity properties in json format so deserializer can 
            // perform its advanced ctor and conversion detection as usual.
            writer.WriteStartObject();

            var idProp = typeof(T).GetProperties()
                .FirstOrDefault(prop => prop.GetCustomAttribute<KeyAttribute>() != null)
                ?? throw new ArgumentException("Entity must have one property annotated with [Key]");

            // Persist the id property with the property name, so it can 
            // be resolved either via the ctor or as a property setter.
            writer.WritePropertyName(idProp.Name);
            writer.WriteValue(entity.RowKey);

            foreach (var property in entity.Properties)
            {
                writer.WritePropertyName(property.Key);
                writer.WriteValue(property.Value.PropertyAsObject);
            }

            writer.WriteEndObject();

            var result = serializer.Deserialize<T>(json.ToString());

            return result;
        }

        async Task<CloudTable> GetTableAsync(string tableName)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();
            return table;
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

            var id = (string?)idProp.GetValue(entity);
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException($"Key property {idProp.Name} cannot be null or empty.");

            return id;
        }
    }

    interface IEntityRepository<T> where T : class
    {
        IAsyncEnumerable<T> GetAllAsync();
        Task<T?> GetAsync(string key);
        Task<T> PutAsync(T entity);
        Task DeleteAsync(T entity);
    }
}

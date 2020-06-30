using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;

namespace NosAyudamos
{
    /// <summary>
    /// An entity repository always stores all entities by using the type full name as the 
    /// partition key and exposing retrieval/deletion methods using just the row key.
    /// </summary>
    /// <typeparam name="T">The type of entity being persisted.</typeparam>
    /// <remarks>
    /// Because the partition key is always the type full name, there is typically no need 
    /// to use a specific table name other than the default <c>Entity</c>.
    /// </remarks>
    class EntityRepository<T> : IEntityRepository<T> where T : class
    {
        static readonly Func<T, string> getRowKey = RowKeyAttribute.CreateAccessor<T>();

        readonly CloudStorageAccount storageAccount;
        readonly ISerializer serializer;
        readonly AsyncLazy<CloudTable> table;

        /// <summary>
        /// Default table name.
        /// </summary>
        public const string DefaultTableName = "Entity";

        public EntityRepository(CloudStorageAccount storageAccount, ISerializer serializer)
            : this(storageAccount, serializer, DefaultTableName) { }

        public EntityRepository(CloudStorageAccount storageAccount, ISerializer serializer, string tableName)
        {
            this.storageAccount = storageAccount;
            this.serializer = serializer;
            table = new AsyncLazy<CloudTable>(() => GetTableAsync(tableName ?? DefaultTableName));
        }

        /// <summary>
        /// Performs a partition scan and retrieves all entities of the same type.
        /// </summary>
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

        public async Task<T?> GetAsync(string rowKey)
        {
            var table = await this.table.GetValueAsync().ConfigureAwait(false);
            var result = await table.ExecuteAsync(TableOperation.Retrieve(typeof(T).FullName!, rowKey))
                .ConfigureAwait(false);

            if (result?.Result == null)
                return default;

            return ToEntity((DynamicTableEntity)result.Result);
        }

        public async Task<T> PutAsync(T entity)
        {
            var rowKey = getRowKey.Invoke(entity);
            var properties = entity.GetType()
                .GetProperties()
                // Persist all properties except for the RowKey itself, since that already has its own column
                .Where(prop => prop.GetCustomAttribute<RowKeyAttribute>() == null)
                .ToDictionary(prop => prop.Name, prop => EntityProperty.CreateEntityPropertyFromObject(prop.GetValue(entity)));

            var table = await this.table.GetValueAsync().ConfigureAwait(false);

            var result = await table.ExecuteAsync(TableOperation.InsertOrReplace(
                new DynamicTableEntity(typeof(T).FullName!, rowKey, "*", properties)))
                .ConfigureAwait(false);

            return ToEntity((DynamicTableEntity)result.Result);
        }

        public async Task DeleteAsync(string rowKey)
        {
            var table = await this.table.GetValueAsync().ConfigureAwait(false);

            await table.ExecuteAsync(TableOperation.Delete(
                new DynamicTableEntity(typeof(T).FullName!, rowKey) { ETag = "*" }))
                .ConfigureAwait(false);
        }

        public async Task DeleteAsync(T entity)
        {
            var rowKey = getRowKey.Invoke(entity);

            var table = await this.table.GetValueAsync().ConfigureAwait(false);

            await table.ExecuteAsync(TableOperation.Delete(
                new DynamicTableEntity(typeof(T).FullName!, rowKey) { ETag = "*" }))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Uses JSON deserialization to convert from the persisted entity data 
        /// to the entity type, so that the right constructor and property 
        /// setters can be invoked, even if they are internal/private.
        /// </summary>
        T ToEntity(DynamicTableEntity entity)
        {
            using var json = new StringWriter();
            using var writer = new JsonTextWriter(json);

            // Write entity properties in json format so deserializer can 
            // perform its advanced ctor and conversion detection as usual.
            writer.WriteStartObject();

            var rowKeyProp = typeof(T).GetProperties()
                .First(prop => prop.GetCustomAttribute<RowKeyAttribute>() != null);

            // Persist the row key property with the property name, so it can 
            // be resolved either via the ctor or as a property setter.
            writer.WritePropertyName(rowKeyProp.Name);
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
            var table = tableClient.GetTableReference(string.IsNullOrEmpty(tableName) ? DefaultTableName : tableName);

            await table.CreateIfNotExistsAsync();
            return table;
        }
    }

    interface IEntityRepository<T> where T : class
    {
        IAsyncEnumerable<T> GetAllAsync();
        Task<T?> GetAsync(string rowKey);
        Task<T> PutAsync(T entity);
        Task DeleteAsync(T entity);
        Task DeleteAsync(string rowKey);
    }
}

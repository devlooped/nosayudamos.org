using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NosAyudamos
{
    /// <summary>
    /// A generic repository that stores entities in table storage, using the properties 
    /// annotated with <see cref="PartitionKeyAttribute"/> and <see cref="RowKeyAttribute"/> 
    /// and optional <see cref="TableAttribute"/>.
    /// </summary>
    /// <typeparam name="T">The type of entity being persisted.</typeparam>
    /// <remarks>
    /// If no <see cref="TableAttribute"/> is provided, entities are persisted in a table 
    /// named after the <typeparamref name="T"/>, without the <c>Entity</c> word (if any).
    /// </remarks>
    class TableRepository<T> : ITableRepository<T> where T : class
    {
        static readonly string DefaultTableName = typeof(T).GetCustomAttribute<TableAttribute>()?.Name ??
            typeof(T).Name.Replace("Entity", "", StringComparison.Ordinal);

        static readonly Func<T, string> getPartitionKey = PartitionKeyAttribute.CreateAccessor<T>();
        static readonly Func<T, string> getRowKey = RowKeyAttribute.CreateAccessor<T>();

        readonly CloudStorageAccount storageAccount;
        readonly ISerializer serializer;
        readonly AsyncLazy<CloudTable> table;

        public TableRepository(CloudStorageAccount storageAccount, ISerializer serializer)
            : this(storageAccount, serializer, DefaultTableName) { }

        public TableRepository(CloudStorageAccount storageAccount, ISerializer serializer, string tableName)
        {
            this.storageAccount = storageAccount;
            this.serializer = serializer;
            table = new AsyncLazy<CloudTable>(() => GetTableAsync(tableName ?? DefaultTableName));
        }

        public async Task<T?> GetAsync(string partitionKey, string rowKey)
        {
            var table = await this.table.GetValueAsync().ConfigureAwait(false);
            var result = await table.ExecuteAsync(TableOperation.Retrieve(partitionKey, rowKey))
                .ConfigureAwait(false);

            if (result?.Result == null)
                return default;

            return ToEntity((DynamicTableEntity)result.Result);
        }

        public async Task<T> PutAsync(T entity)
        {
            var partitionKey = getPartitionKey.Invoke(entity);
            var rowKey = getRowKey.Invoke(entity);
            var properties = entity.GetType()
                .GetProperties()
                // Persist all properties except for the key properties, since those already have their own column
                .Where(prop => prop.GetCustomAttribute<PartitionKeyAttribute>() == null && prop.GetCustomAttribute<RowKeyAttribute>() == null)
                .ToDictionary(prop => prop.Name, prop => EntityProperty.CreateEntityPropertyFromObject(prop.GetValue(entity)));

            var table = await this.table.GetValueAsync().ConfigureAwait(false);

            var result = await table.ExecuteAsync(TableOperation.InsertOrReplace(
                new DynamicTableEntity(partitionKey, rowKey, "*", properties)))
                .ConfigureAwait(false);

            return ToEntity((DynamicTableEntity)result.Result);
        }

        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            var table = await this.table.GetValueAsync().ConfigureAwait(false);

            await table.ExecuteAsync(TableOperation.Delete(
                new DynamicTableEntity(partitionKey, rowKey) { ETag = "*" }))
                .ConfigureAwait(false);
        }

        public async Task DeleteAsync(T entity)
        {
            var partitionKey = getPartitionKey.Invoke(entity);
            var rowKey = getRowKey.Invoke(entity);

            var table = await this.table.GetValueAsync().ConfigureAwait(false);

            await table.ExecuteAsync(TableOperation.Delete(
                new DynamicTableEntity(partitionKey, rowKey) { ETag = "*" }))
                .ConfigureAwait(false);
        }

        async Task<CloudTable> GetTableAsync(string tableName)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(string.IsNullOrEmpty(tableName) ? DefaultTableName : tableName);

            await table.CreateIfNotExistsAsync();
            return table;
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

            var partitionKeyProp = typeof(T).GetProperties()
                .First(prop => prop.GetCustomAttribute<PartitionKeyAttribute>() != null);
            var rowKeyProp = typeof(T).GetProperties()
                .First(prop => prop.GetCustomAttribute<RowKeyAttribute>() != null);

            // Persist the key properties with the property name, so they can 
            // be resolved either via the ctor or as a property setter.

            writer.WritePropertyName(partitionKeyProp.Name);
            writer.WriteValue(entity.PartitionKey);

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
    }

    public interface ITableRepository<T> where T : class
    {
        Task<T?> GetAsync(string partitionKey, string rowKey);
        Task<T> PutAsync(T entity);
        Task DeleteAsync(T entity);
        Task DeleteAsync(string partitionKey, string rowKey);
    }
}

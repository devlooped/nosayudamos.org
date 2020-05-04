using System;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    class DataEntity : TableEntity
    {
        public static DataEntity Create<T>(string partitionKey, T data, ISerializer serializer) =>
            new DataEntity(partitionKey, typeof(T).FullName!)
            {
                Data = serializer.Serialize(data ?? throw new ArgumentNullException(nameof(data))),
                DataVersion = (typeof(T).Assembly.GetName().Version ?? new Version(1, 0)).ToString(2)
            };

        public DataEntity() { }

        public DataEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
        }

        public string? Data { get; set; }
        public string? DataVersion { get; set; }
        public int Version { get; set; }
    }
}

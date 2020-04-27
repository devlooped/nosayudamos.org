using System;
using Newtonsoft.Json;

namespace NosAyudamos
{
    class Serializer : ISerializer
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
        };

        public T Deserialize<T>(object data)
            => JsonConvert.DeserializeObject<T>(data?.ToString() ?? throw new ArgumentNullException(nameof(data)), settings)!;

        public object Serialize<T>(T value)
            => JsonConvert.SerializeObject(value, settings);
    }

    interface ISerializer
    {
        object Serialize<T>(T value);

        T Deserialize<T>(object data);
    }
}

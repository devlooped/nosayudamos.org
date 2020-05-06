using System;
using System.Composition;
using Newtonsoft.Json;

namespace NosAyudamos
{
    [Shared]
    class Serializer : ISerializer
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
#if DEBUG
            Formatting = Formatting.Indented,
#endif
        };

        public object Deserialize(string data, Type type)
            => JsonConvert.DeserializeObject(data?.ToString() ?? throw new ArgumentNullException(nameof(data)), type, settings)!;

        public T Deserialize<T>(string data)
            => JsonConvert.DeserializeObject<T>(data?.ToString() ?? throw new ArgumentNullException(nameof(data)), settings)!;

        public string Serialize(object value)
            => JsonConvert.SerializeObject(value, settings);
    }

    interface ISerializer
    {
        string Serialize(object value);

        T Deserialize<T>(string data);

        object Deserialize(string data, Type type);
    }
}

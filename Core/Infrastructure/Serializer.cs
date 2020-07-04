using System;
using System.Composition;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NosAyudamos
{
    [Shared]
    class Serializer : ISerializer
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Converters =
            {
                new IsoDateTimeConverter
                {
                    DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK",
                    DateTimeStyles = DateTimeStyles.AdjustToUniversal
                },
            },
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK",
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        public object Deserialize(string data, Type type)
            => JsonConvert.DeserializeObject(data ?? throw new ArgumentNullException(nameof(data)), type, settings)!;

        public T Deserialize<T>(string data)
            => JsonConvert.DeserializeObject<T>(data ?? throw new ArgumentNullException(nameof(data)), settings)!;

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

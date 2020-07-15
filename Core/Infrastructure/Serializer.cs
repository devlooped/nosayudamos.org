using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

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

        public IDictionary<string, string> Deserialize(string data)
        {
            var json = (JObject)JsonConvert.DeserializeObject(data, settings)!;
            var results = json
                .Descendants()
                .Where(token => !token.Children().Any())
                .Aggregate(new Dictionary<string, string>(), (props, token) =>
                {
                    props.Add(token.Path, token.ToString());
                    return props;
                });

            return results;
        }

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

        IDictionary<string, string> Deserialize(string data);

        object Deserialize(string data, Type type);
    }
}

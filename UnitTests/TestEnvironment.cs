using System.ComponentModel;
using System.IO;
using Newtonsoft.Json.Linq;

namespace NosAyudamos
{
    class TestEnvironment : IEnvironment
    {
        Environment env = new Environment();
        JObject values;

        public static bool CanRunFeatures { get; } = File.Exists("local.settings.json");

        public TestEnvironment()
        {
            if (File.Exists("local.settings.json"))
            {
                values = JObject.Parse(File.ReadAllText("local.settings.json")).Value<JObject>("Values");
            }
            else
            {
                values = new JObject();
            }

            values.Add("TEST", bool.TrueString);
        }

        public string GetVariable(string name)
        {
            if (values.TryGetValue(name, out var value))
                return value.ToString();
            else
                return env.GetVariable(name);
        }

        public T GetVariable<T>(string name, T defaultValue = default)
        {
            var value = values.TryGetValue(name, out var token) ?
                token.ToString() : System.Environment.GetEnvironmentVariable(name);

            if (value != null)
            {
                if (value is T typed)
                    return typed;

                var converter = TypeDescriptor.GetConverter(typeof(T));

                return (T)converter.ConvertFromString(value);
            }

            return defaultValue;
        }
    }
}

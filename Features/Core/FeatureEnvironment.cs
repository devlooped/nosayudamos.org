using System.IO;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace NosAyudamos
{
    class FeatureEnvironment : IEnvironment
    {
        Environment env = new Environment(new MemoryCache(new MemoryCacheOptions()));

        public FeatureEnvironment()
        {
            if (File.Exists("local.settings.json"))
            {
                var values = JObject.Parse(File.ReadAllText("local.settings.json")).Value<JObject>("Values");

                foreach (var value in values)
                {
                    System.Environment.SetEnvironmentVariable(value.Key, value.Value.ToString());
                }

                System.Environment.SetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT", "Development");
                System.Environment.SetEnvironmentVariable("TESTING", bool.TrueString);
            }
        }

        public string GetVariable(string name)
        {
            return env.GetVariable(name);
        }

        public T GetVariable<T>(string name, T defaultValue = default)
        {
            return env.GetVariable<T>(name, defaultValue);
        }
    }
}

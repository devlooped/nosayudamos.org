using System;
using System.ComponentModel;
using System.Composition;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;

namespace NosAyudamos
{
    static class EnvironmentExtensions
    {
        /// <summary>
        /// Whether azure functions are running in development mode, locally.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static bool IsDevelopment(this IEnvironment environment)
            => IsTesting(environment) || environment.GetVariable("AZURE_FUNCTIONS_ENVIRONMENT", "Production") == "Development";

        /// <summary>
        /// Whether the code is being run from a test.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static bool IsTesting(this IEnvironment environment)
            => environment.GetVariable("TESTING", false);
    }

    [Shared]
    class Environment : IEnvironment
    {
        const string KV = "kv-";

        IMemoryCache cache;
        Lazy<SecretClient> client;

        public Environment(IMemoryCache cache)
        {
            this.cache = cache;
            client = new Lazy<SecretClient>(() => new SecretClient(new Uri(GetVariable("KeyVaultUrl")), new DefaultAzureCredential()));
        }

        public string GetVariable(string name)
        {
            var value = Ensure.NotEmpty(
                    System.Environment.GetEnvironmentVariable(
                        Ensure.NotEmpty(name, nameof(name))), name);

            if (IsDev())
            {
                return value.StartsWith(KV, StringComparison.OrdinalIgnoreCase) ? GetSecret(value) : value;
            }

            return value;
        }

        public T GetVariable<T>(string name, T defaultValue = default)
        {
            var value = System.Environment.GetEnvironmentVariable(
                        Ensure.NotEmpty(name, nameof(name)));

            if (value != null)
            {
                if (IsDev())
                {
                    value = value.StartsWith(KV, StringComparison.OrdinalIgnoreCase) ? GetSecret(value) : value;
                }

                if (value is T typed)
                    return typed;

                var converter = TypeDescriptor.GetConverter(typeof(T));

                return (T)converter.ConvertFromString(value);
            }

            return defaultValue;
        }

        private string GetSecret(string name)
        {
            if (!cache.TryGetValue(name, out string value))
            {
                var secret = client.Value.GetSecret(name);
                var val = secret?.Value?.Value ?? throw new ArgumentException(name);
                cache.Set(name, val);

                value = val;
            }

            return value;
        }

        private bool IsDev()
        {
            var value = System.Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");

            value ??= "Production";

            return value == "Development";
        }
    }

    interface IEnvironment
    {
        string GetVariable(string name);
        T GetVariable<T>(string name, T defaultValue = default);
    }
}

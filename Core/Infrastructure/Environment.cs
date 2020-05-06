using System.ComponentModel;
using System.Composition;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

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
        readonly IConfiguration config;

        public Environment()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // In locally run tests, the file will be alongside the assembly.
            // In azure, it will be one level up.
            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
                basePath = new DirectoryInfo(basePath).Parent.FullName;

            var builder = new ConfigurationBuilder()
                 .SetBasePath(basePath)
                 .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                 .AddJsonFile("secrets.settings.json", optional: true, reloadOnChange: true)
                 .AddJsonFile("tests.settings.json", optional: true, reloadOnChange: true)
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddEnvironmentVariables();

            var config = builder.Build();

            // Use the config above to initialize the keyvault config extension.
            builder.AddAzureKeyVault(
                $"https://{config["AzureKeyVaultName"]}.vault.azure.net/",
                config["AZURE_CLIENT_ID"],
                config["AZURE_CLIENT_SECRET"]);

            // Now build the final version that includes the keyvault provider.
            this.config = builder.Build();
        }

        public string GetVariable(string name) =>
            Ensure.NotEmpty(config[Ensure.NotEmpty(name, nameof(name))], name);

        public T GetVariable<T>(string name, T defaultValue = default)
        {
            var value = config[Ensure.NotEmpty(name, nameof(name))];

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

    interface IEnvironment
    {
        string GetVariable(string name);
        T GetVariable<T>(string name, T defaultValue = default);
    }
}

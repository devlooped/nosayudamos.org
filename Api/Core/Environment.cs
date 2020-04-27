using System.ComponentModel;
using System.Composition;

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
        public string GetVariable(string name)
        {
            return Ensure.NotEmpty(
                    System.Environment.GetEnvironmentVariable(
                        Ensure.NotEmpty(name, nameof(name))), name);
        }

        public T GetVariable<T>(string name, T defaultValue = default)
        {
            var value = System.Environment.GetEnvironmentVariable(
                        Ensure.NotEmpty(name, nameof(name)));

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

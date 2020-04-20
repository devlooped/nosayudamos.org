using System;
using System.ComponentModel;

namespace NosAyudamos
{
    interface IEnvironment
    {
        string GetVariable(string name);
        T GetVariable<T>(string name, T defaultValue = default);
    }

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
                var converter = TypeDescriptor.GetConverter(typeof(T));

                return (T)converter.ConvertFromString(value);
            }

            return defaultValue;
        }
    }
}

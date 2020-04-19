using System;
using System.ComponentModel;

namespace NosAyudamos
{
    public interface IEnviroment
    {
        string GetVariable(string name);
        T GetVariable<T>(string name, T defaultValue = default);
    }

    class Enviroment : IEnviroment
    {
        public string GetVariable(string name)
        {
            return Ensure.NotEmpty(
                    Environment.GetEnvironmentVariable(
                        Ensure.NotEmpty(name, nameof(name))), name);
        }

        public T GetVariable<T>(string name, T defaultValue = default)
        {
            var value = Environment.GetEnvironmentVariable(
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

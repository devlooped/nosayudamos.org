using System.Collections.Generic;
using System.ComponentModel;

namespace NosAyudamos
{
    class TestEnvironment : IEnvironment
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        Environment environment = new Environment();

        public string GetVariable(string name)
        {
            if (values.TryGetValue(name, out var value))
                return value;

            return environment.GetVariable(name);
        }

        public T GetVariable<T>(string name, T defaultValue = default)
        {
            if (values.TryGetValue(name, out var value))
            {
                if (value is T typed)
                    return typed;

                var converter = TypeDescriptor.GetConverter(typeof(T));

                return (T)converter.ConvertFromString(value);
            }

            return environment.GetVariable(name, defaultValue);

        }

        public void SetVariable(string name, string value)
            => values[name] = value;
    }
}

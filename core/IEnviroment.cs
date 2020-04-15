using System;

namespace NosAyudamos
{
    public interface IEnviroment
    {
        string GetVariable(string name);
    }

    public class Enviroment : IEnviroment
    {
        public string GetVariable(string name)
        {
            Exceptions.ThrowIfNullOrEmpty(name, nameof(name));

            return Environment.GetEnvironmentVariable(name);
        }
    }
}
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
            return Ensure.NotEmpty(
                    Environment.GetEnvironmentVariable(
                        Ensure.NotEmpty(name, nameof(name))), name);
        }
    }
}
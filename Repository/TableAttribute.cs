using System;

namespace NosAyudamos
{
    [AttributeUsage(System.AttributeTargets.Class)]
    class TableAttribute : Attribute
    {
        public TableAttribute(string name) => Name = name;

        public string Name { get; }
    }
}

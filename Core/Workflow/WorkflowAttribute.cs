using System;

namespace NosAyudamos
{
    [AttributeUsage(AttributeTargets.Class)]
    class WorkflowAttribute : Attribute
    {
        public WorkflowAttribute(string? name = default) => Name = name;
        public string? Name { get; }
    }
}

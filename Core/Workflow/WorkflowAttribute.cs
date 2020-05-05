using System;

namespace NosAyudamos
{
    [AttributeUsage(AttributeTargets.Class)]
    class WorkflowAttribute : Attribute
    {
        public WorkflowAttribute(string name) => Name = name;
        public string Name { get; }
    }
}

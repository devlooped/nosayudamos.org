using System;
using Microsoft.Azure.WebJobs.Description;

namespace NosAyudamos
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    class ResolveAttribute : Attribute
    {
        public ResolveAttribute(string? name = default) => Name = name;

        public string? Name { get; }
    }
}

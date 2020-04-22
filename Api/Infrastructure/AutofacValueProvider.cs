using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace NosAyudamos.Infrastructure
{
    class AutofacValueProvider : IValueProvider
    {
        readonly IContainer container;
        private string? name;

        public AutofacValueProvider(IContainer container, Type type, string? name)
        {
            this.container = container;
            this.name = name;
            Type = type;
        }

        public Type Type { get; }

        public Task<object> GetValueAsync()
        {
            if (name == null)
                return Task.FromResult(container.Resolve(Type));
            else
                return Task.FromResult(container.ResolveNamed(name, Type));
        }

        public string ToInvokeString() => name == null ? $"Resolve({Type.Name})" : $"Resolve({name}, {Type.Name})";
    }
}

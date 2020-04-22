using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace NosAyudamos.Infrastructure
{
    class AutofacBinding : IBinding
    {
        readonly IContainer container;
        readonly ParameterInfo parameter;
        readonly ResolveAttribute resolve;

        public AutofacBinding(IContainer container, ParameterInfo parameter)
        {
            this.container = container;
            this.parameter = parameter;
            resolve = parameter.GetCustomAttribute<ResolveAttribute>()!;
        }

        public bool FromAttribute => true;

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
            => Task.FromResult<IValueProvider>(new AutofacValueProvider(container, parameter.ParameterType, resolve.Name));

        public Task<IValueProvider> BindAsync(BindingContext context)
            => Task.FromResult<IValueProvider>(new AutofacValueProvider(container, parameter.ParameterType, resolve.Name));

        public ParameterDescriptor ToParameterDescriptor()
            => new ParameterDescriptor
            {
                Name = parameter.Name,
                Type = parameter.ParameterType.FullName
            };
    }
}

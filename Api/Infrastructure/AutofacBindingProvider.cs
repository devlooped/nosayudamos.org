using System.Threading.Tasks;
using Autofac;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace NosAyudamos.Infrastructure
{
    class AutofacBindingProvider : IBindingProvider
    {
        readonly IContainer container;

        public AutofacBindingProvider(IContainer container) => this.container = container;

        public Task<IBinding> TryCreateAsync(BindingProviderContext context) => Task.FromResult<IBinding>(new AutofacBinding(container, context.Parameter));
    }
}

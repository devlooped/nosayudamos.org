using Autofac;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;

namespace NosAyudamos.Infrastructure
{
    class AutofacExtension : IExtensionConfigProvider
    {
        readonly IBindingProvider provider;

        public AutofacExtension(IContainer container) => provider = new AutofacBindingProvider(container);

        public void Initialize(ExtensionConfigContext context) => context.AddBindingRule<ResolveAttribute>().Bind(provider);
    }
}

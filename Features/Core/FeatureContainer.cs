using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Core.Resolving;
using Microsoft.Extensions.DependencyInjection;

namespace NosAyudamos
{
    /// <summary>
    /// Scenarios can use a container by directly declaring it as a ctor 
    /// parameter.
    /// </summary>
    public sealed class FeatureContainer : IContainer
    {
        /// <summary>
        /// The static constructor is the only one that validates the entire 
        /// container and all registrations, since that's somewhat expensive.
        /// </summary>
        static FeatureContainer()
        {
            var builder = CreateBuilder();
            IContainer container = null;

            builder.Register(c => container).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new ComponentServiceProvider(container)).As<IServiceProvider>().SingleInstance();

            container = builder.Build();
            Verify(container);
        }

        IContainer container;

        public FeatureContainer()
        {
            var builder = CreateBuilder();

            builder.Register(c => container).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new ComponentServiceProvider(container)).As<IServiceProvider>().SingleInstance();

            container = builder.Build();
        }

        static ContainerBuilder CreateBuilder()
        {
            var builder = new ContainerBuilder();
            var services = new ServiceCollection();

            var testServices = new HashSet<Type>
            {
                typeof(HttpClient),
            };

            new Startup().Configure(services, new Environment());

            var candidate = services.Where(desc =>
                !testServices.Contains(desc.ServiceType) &&
                !testServices.Contains(desc.ImplementationType)).ToList();

            // Register in container all the services except for the test replacements, 
            // and make them singletons since each test run will get its own container (for now?)
            foreach (var group in candidate.GroupBy(desc => desc.ImplementationType))
            {
                if (group.Key == null)
                {
                    foreach (var service in group)
                    {
                        if (service.ImplementationInstance != null)
                            builder.RegisterInstance(service.ImplementationInstance).As(service.ServiceType).SingleInstance();
                        else if (service.ImplementationFactory != null)
                            builder.Register(c => service.ImplementationFactory(c.Resolve<IServiceProvider>())).As(service.ServiceType).SingleInstance();
                        else if (service.ServiceType.IsGenericTypeDefinition)
                            builder.RegisterGeneric(service.ImplementationType).As(service.ServiceType).SingleInstance();
                        else if (service.ServiceType.IsAssignableFrom(service.ImplementationType))
                            builder.RegisterType(service.ImplementationType).As(service.ServiceType).SingleInstance();
                    }
                }
                else
                {
                    var asTypes = group.Select(desc => desc.ServiceType).ToArray();
                    var registered = false;

                    foreach (var instances in group.GroupBy(desc => desc.ImplementationInstance).Where(ig => ig.Key != null))
                    {
                        builder.RegisterInstance(instances.Key).As(asTypes).SingleInstance();
                        registered = true;
                    }

                    foreach (var factories in group.GroupBy(desc => desc.ImplementationFactory).Where(fg => fg.Key != null))
                    {
                        builder.Register(c => factories.Key(c.Resolve<IServiceProvider>())).As(asTypes).SingleInstance();
                        registered = true;
                    }

                    if (!registered)
                    {
                        if (group.Key.IsGenericTypeDefinition)
                            builder.RegisterGeneric(group.Key).As(asTypes).SingleInstance();
                        else
                            builder.RegisterType(group.Key).As(asTypes).SingleInstance();
                    }
                }
            }

            // For some reason, the built-in registrations we were providing via Startup for HttpClient weren't working.
            builder.RegisterType<HttpClient>().InstancePerDependency();

            return builder;
        }

        static void Verify(IContainer container)
        {
            var errors = new List<Exception>();
            var registrations = container.ComponentRegistry.Registrations;

            // We'll only verify the registration for our own assembly types
            var assemblies = new HashSet<Assembly>
            {
                typeof(Startup).Assembly,
                Assembly.GetExecutingAssembly(),
            };

            using (var scope = container.BeginLifetimeScope())
            {
                foreach (var registration in registrations.SelectMany(x => x.Services))
                {
                    try
                    {
                        if (registration is TypedService tSvc &&
                            assemblies.Contains(tSvc.ServiceType.Assembly))
                        {
                            scope.Resolve(tSvc.ServiceType);
                        }
                        else if (registration is KeyedService kSvc &&
                            assemblies.Contains(kSvc.ServiceType.Assembly))
                        {
                            scope.ResolveNamed(kSvc.ServiceKey.ToString(), kSvc.ServiceType);
                        }
                    }
                    catch (DependencyResolutionException ex)
                    {
                        errors.Add(ex);
                    }
                }
            }

            if (errors.Count != 0)
                throw new AggregateException(errors.ToArray());
        }

        class ComponentServiceProvider : IServiceProvider
        {
            readonly IComponentContext context;

            public ComponentServiceProvider(IComponentContext context) => this.context = context;

            public object GetService(Type serviceType)
            {
                if (context.TryResolve(serviceType, out var service))
                    return service;

                return null;
            }
        }

        public IDisposer Disposer => container.Disposer;

        public object Tag => container.Tag;

        public IComponentRegistry ComponentRegistry => container.ComponentRegistry;

        public event EventHandler<LifetimeScopeBeginningEventArgs> ChildLifetimeScopeBeginning
        {
            add => container.ChildLifetimeScopeBeginning += value;
            remove => container.ChildLifetimeScopeBeginning -= value;
        }

        public event EventHandler<LifetimeScopeEndingEventArgs> CurrentScopeEnding
        {
            add => container.CurrentScopeEnding += value;
            remove => container.CurrentScopeEnding -= value;
        }

        public event EventHandler<ResolveOperationBeginningEventArgs> ResolveOperationBeginning
        {
            add => container.ResolveOperationBeginning += value;
            remove => container.ResolveOperationBeginning -= value;
        }

        public ILifetimeScope BeginLifetimeScope() => container.BeginLifetimeScope();

        public ILifetimeScope BeginLifetimeScope(object tag) => container.BeginLifetimeScope(tag);

        public ILifetimeScope BeginLifetimeScope(Action<ContainerBuilder> configurationAction) => container.BeginLifetimeScope(configurationAction);

        public ILifetimeScope BeginLifetimeScope(object tag, Action<ContainerBuilder> configurationAction) => container.BeginLifetimeScope(tag, configurationAction);

        public void Dispose() => container.Dispose();

        public ValueTask DisposeAsync() => container.DisposeAsync();

        public object ResolveComponent(ResolveRequest request) => container.ResolveComponent(request);
    }
}

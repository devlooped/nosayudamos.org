using System.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Slack;
using Polly;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.IO;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.Threading;
using Microsoft.Extensions.Logging;

[assembly: WebJobsStartup(typeof(NosAyudamos.Startup))]

namespace NosAyudamos
{
    class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            // testable service registrations in the test-invoked method.
#pragma warning disable CA2000 // Dispose objects before losing scope
            Configure(builder.Services, new Environment());
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        internal void Configure(IServiceCollection services, IEnvironment env)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(Assembly.GetExecutingAssembly().GetCustomAttribute<NeutralResourcesLanguageAttribute>()!.CultureName);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;

            var config = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Verbose()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Host", LogEventLevel.Warning)
                .MinimumLevel.Override("Function", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Azure", LogEventLevel.Warning)
                .Enrich.FromLogContext();

            var seqUrl = env.GetVariable("SeqUrl", default(string));
            if (!string.IsNullOrEmpty(seqUrl))
                config.WriteTo.Seq(seqUrl);

            if (!env.IsDevelopment())
            {
                //Add ApplicationInsights in production only
                services.AddLogging(builder => builder.AddApplicationInsights(env.GetVariable("ApplicationInsightsKey")));
                services.AddApplicationInsightsTelemetry(env.GetVariable("ApplicationInsightsKey"));
            }
            else
            {
                //Add Serilog (Console) in development and testing
                config.WriteTo.Console();
            }

            if (!env.IsTesting())
            {
                //Add Serilog (Slack) in production and development
                config.WriteTo.Logger(lc => lc.Filter
                     .ByIncludingOnly(e => e.Properties.ContainsKey("Category"))
                     .WriteTo.Slack(new SlackSinkOptions
                     {
                         WebHookUrl = env.GetVariable("SlackLogWebHook"),
                         CustomChannel = "#api",
                         ShowDefaultAttachments = false,
                         ShowPropertyAttachments = false,
                         ShowExceptionAttachments = true,
                     }, restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: @"`{Category}:{Level}` ```{@Message:j}```"));
            }

            var logger = config.CreateLogger();

            services.AddLogging(builder => builder.AddSerilog(logger));

            services.AddSingleton(env);

            // DI conventions are:
            // 1. Candidates: types that implement at least one interface
            // 2. Looking at its attributes: if they don't have [Shared], they are registered as transient
            // 3. Optionally can have [Export] to force registration of a type without interfaces
            var candidateTypes = typeof(DomainObject).Assembly.GetTypes()
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsGenericTypeDefinition &&
                    !t.IsValueType &&
                    // Omit explicitly opted-out components
                    t.GetCustomAttribute<NoExportAttribute>(true) == null &&
                    // Omit generated types like local state capturing
                    t.GetCustomAttribute<CompilerGeneratedAttribute>() == null &&
                    // Omit generated types for async state machines
                    !t.GetInterfaces().Any(i => i == typeof(IAsyncStateMachine)))
                .Where(t => t.GetInterfaces().Length > 0 || t.GetCustomAttribute<ExportAttribute>() != null);

            foreach (var implementationType in candidateTypes)
            {
                var singleton = implementationType.GetCustomAttribute<SharedAttribute>() != null;
                foreach (var serviceType in implementationType.GetInterfaces())
                {
                    if (singleton)
                        services.AddSingleton(serviceType, implementationType);
                    else
                        services.AddScoped(serviceType, implementationType);
                }

                if (singleton)
                    services.AddSingleton(implementationType);
                else
                    services.AddScoped(implementationType);
            }

            var registry = new Resiliency(env).GetRegistry();
            var policy = registry.Get<IAsyncPolicy<HttpResponseMessage>>("HttpClientPolicy");

            if (!env.IsTesting())
            {
                services.AddHttpClient<IMessaging, Messaging>().AddPolicyHandler(policy);
                services.AddHttpClient<IPersonalIdRecognizer, PersonalIdRecognizer>().AddPolicyHandler(policy);
                services.AddHttpClient<IQRCode, QRCode>().AddPolicyHandler(policy);
                services.AddHttpClient<StartupWorkflow>().AddPolicyHandler(policy);
                services.AddHttpClient<ChatApiMessaging>().AddPolicyHandler(policy);
            }

            if (env.IsDevelopment())
                services.AddSingleton(CloudStorageAccount.DevelopmentStorageAccount);
            else
                services.AddSingleton(CloudStorageAccount.Parse(env.GetVariable("StorageConnectionString")));

            services.AddScoped(typeof(IEntityRepository<>), typeof(EntityRepository<>));
            services.AddScoped(typeof(ITableRepository<>), typeof(TableRepository<>));
            services.Decorate<IPersonRepository, EventGridPersonRepository>();
            services.Decorate<IRequestRepository, EventGridRequestRepository>();

#pragma warning disable CA2000 // Dispose objects before losing scope: This factory is intended to live for the duration of the app.
            var jtc = new JoinableTaskContext();
#pragma warning restore CA2000 // Dispose objects before losing scope
            services.AddSingleton(jtc);
            services.AddSingleton(jtc.Factory);

            services.AddPolicyRegistry(registry);
            services.AddMemoryCache();
        }
    }
}

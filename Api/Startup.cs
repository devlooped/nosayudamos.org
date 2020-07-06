using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Polly;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Slack;

[assembly: WebJobsStartup(typeof(NosAyudamos.Startup))]

namespace NosAyudamos
{
    class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            // testable service registrations in the test-invoked method.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var env = new Environment();
            try
            {
                Configure(builder.Services, env);
            }
            catch (Exception e)
            {
                TraceException(e, env);
                throw;
            }
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
                services.AddLogging(builder => builder.AddApplicationInsights(env.GetVariable("APPINSIGHTS_INSTRUMENTATIONKEY")));
                services.AddApplicationInsightsTelemetry(env.GetVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

                var liveMetricsKey = env.GetVariable("APPINSIGHTS_QUICKPULSEAUTHAPIKEY", default(string));
                if (!string.IsNullOrEmpty(liveMetricsKey))
                {
                    services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, o)
                        => module.AuthenticationApiKey = liveMetricsKey);
                }
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
                     }, restrictedToMinimumLevel: LogEventLevel.Warning, outputTemplate: @"`{Category}:{Level}` ```{@Message:j}```"));
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

        static void TraceException(Exception e, IEnvironment env)
        {
            Console.WriteLine(e.ToString());
            SendTelemetry(e, env);
            SendEvent(e, env);
        }

        static void SendTelemetry(Exception e, IEnvironment env)
        {
            var aiKey = env.GetVariable("APPINSIGHTS_INSTRUMENTATIONKEY", default(string));
            if (string.IsNullOrEmpty(aiKey))
                return;

            using var config = new TelemetryConfiguration(aiKey);
            var client = new TelemetryClient(config);

            client.TrackException(new ExceptionTelemetry(e)
            {
                SeverityLevel = SeverityLevel.Critical
            });

            client.Flush();
        }

        static void SendEvent(Exception e, IEnvironment env)
        {
            var gridUrl = env.GetVariable("EventGridUrl", default(string));
            var gridKey = env.GetVariable("EventGridAccessKey", default(string));
            if (string.IsNullOrEmpty(gridUrl) ||
                string.IsNullOrEmpty(gridKey))
                return;

            var credentials = new TopicCredentials(gridKey);
            var domain = new Uri(gridUrl).Host;
            using var client = new EventGridClient(credentials);

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            var now = DateTime.UtcNow;
            client.PublishEventsAsync(domain, new List<EventGridEvent>
            {
                new EventGridEvent
                {
                    Id = now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                    EventType = "System.Exception",
                    EventTime = now,
                    Data = new Serializer().Serialize(e),
                    DataVersion = typeof(Startup).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
                        typeof(Startup).Assembly.GetName().Version?.ToString(),

                    Subject = e.Message,
                    Topic = "Runtime",
                }
            }).Wait();
        }

    }
}

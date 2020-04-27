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
using AutoMapper;
using System.Runtime.CompilerServices;
using NosAyudamos.Functions;
using NosAyudamos.Properties;
using System.IO;

[assembly: WebJobsStartup(typeof(NosAyudamos.Startup))]

namespace NosAyudamos
{
    class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            // non-testable services are registered here.
            builder.Services.AddApplicationInsightsTelemetry();

            // testable service registrations in the test-invoked method.
            Configure(builder.Services, new Environment());
        }

        internal void Configure(IServiceCollection services, IEnvironment environment)
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
                .Enrich.FromLogContext()
                .WriteTo.Console();

            if (environment.IsTesting())
            {
                if (File.Exists("log.txt"))
                {
                    try
                    { File.Delete("log.txt"); }
                    catch (IOException) { }
                }

                config.MinimumLevel.Verbose()
                    .WriteTo.File("log.txt");
            }

            if (!environment.IsTesting())
            {
                config.WriteTo.Logger(lc => lc.Filter
                    .ByIncludingOnly(e => e.Properties.ContainsKey("Category"))
                    .WriteTo.Slack(new SlackSinkOptions
                    {
                        WebHookUrl = environment.GetVariable("SlackLogWebHook"),
                        CustomChannel = "#api",
                        ShowDefaultAttachments = false,
                        ShowPropertyAttachments = false,
                        ShowExceptionAttachments = true,
                    }, restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: @"`{Category}:{Level}` ```{@Message:j}```"));
            }

            var logger = config.CreateLogger();

            Log.Information(Strings.Startup.Starting);

            services.AddSingleton<ILogger>(logger);
            services.AddLogging(lb => lb.AddSerilog(logger));

            // DI conventions are:
            // 1. Candidates: types that implement at least one interface
            // 2. Looking at its attributes: if they don't have [Shared], they are registered as transient
            // 3. Optionally can have [Export] to force registration of a type without interfaces
            var candidateTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsGenericTypeDefinition &&
                    !t.IsValueType &&
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

            var registry = new Resiliency(environment).GetRegistry();
            var policy = registry.Get<IAsyncPolicy<HttpResponseMessage>>("HttpClientPolicy");

            if (!environment.IsTesting())
            {
                services.AddHttpClient<IMessaging, Messaging>().AddPolicyHandler(policy);
                services.AddHttpClient<IPersonRecognizer, PersonRecognizer>().AddPolicyHandler(policy);
                services.AddHttpClient<IQRCode, QRCode>().AddPolicyHandler(policy);
                services.AddHttpClient<IStartupWorkflow, StartupWorkflow>().AddPolicyHandler(policy);
                services.AddHttpClient<ChatApi>().AddPolicyHandler(policy);
            }

            services.AddPolicyRegistry(registry);
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
        }
    }
}

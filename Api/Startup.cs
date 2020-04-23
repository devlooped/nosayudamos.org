using System.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NosAyudamos.Properties;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Slack;
using Polly;
using System.Net.Http;
using AutoMapper;

[assembly: WebJobsStartup(typeof(NosAyudamos.Startup))]

namespace NosAyudamos
{
    class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(Assembly.GetExecutingAssembly().GetCustomAttribute<NeutralResourcesLanguageAttribute>()!.CultureName);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;

            var logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Host", LogEventLevel.Warning)
                .MinimumLevel.Override("Function", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Azure", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Logger(lc => lc.Filter
                    .ByIncludingOnly(e => e.Properties.ContainsKey("Category"))
                    .WriteTo.Slack(new SlackSinkOptions
                    {
                        WebHookUrl = System.Environment.GetEnvironmentVariable("SlackApiWebHook"),
                        CustomChannel = "#api",
                        ShowDefaultAttachments = false,
                        ShowPropertyAttachments = false,
                        ShowExceptionAttachments = true,
                    }, restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: @"`{Category}:{Level}` ```{@Message:j}```"))
                .WriteTo.Console()
                .CreateLogger();

            Log.Information(Strings.Startup.Starting);

            builder.Services.AddLogging(lb => lb.AddSerilog(logger));
            builder.Services.AddApplicationInsightsTelemetry();

            // DI conventions are:
            // 1. Candidates: types that implement at least one interface
            // 2. Looking at its attributes: if they don't have [Shared], they are registered as transient
            // 3. Optionally can have [Export] to force registration of a type without interfaces
            var candidateTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => t.GetInterfaces().Length > 0 || t.GetCustomAttribute<ExportAttribute>() != null);

            foreach (var implementationType in candidateTypes)
            {
                var singleton = implementationType.GetCustomAttribute<SharedAttribute>() != null;
                foreach (var serviceType in implementationType.GetInterfaces())
                {
                    if (singleton)
                        builder.Services.AddSingleton(serviceType, implementationType);
                    else
                        builder.Services.AddScoped(serviceType, implementationType);
                }

                if (singleton)
                    builder.Services.AddSingleton(implementationType);
                else
                    builder.Services.AddScoped(implementationType);
            }

            var registry = new Resiliency(new Environment()).GetRegistry();
            var policy = registry.Get<IAsyncPolicy<HttpResponseMessage>>("HttpClientPolicy");

            builder.Services.AddHttpClient<IMessaging, Messaging>().AddPolicyHandler(policy);
            builder.Services.AddHttpClient<IPersonRecognizer, PersonRecognizer>().AddPolicyHandler(policy);
            builder.Services.AddHttpClient<IQRCode, QRCode>().AddPolicyHandler(policy);
            builder.Services.AddHttpClient<IStartupWorkflow, StartupWorkflow>().AddPolicyHandler(policy);
            builder.Services.AddHttpClient<ChatApi>().AddPolicyHandler(policy);

            builder.Services.AddPolicyRegistry(registry);
            builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
        }
    }
}

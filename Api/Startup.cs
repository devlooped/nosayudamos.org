using System.Globalization;
using System.Reflection;
using System.Resources;
using Autofac;
using Autofac.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NosAyudamos.Infrastructure;
using NosAyudamos.Properties;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Slack;

[assembly: WebJobsStartup(typeof(NosAyudamos.Startup))]

namespace NosAyudamos
{

    class Startup : IWebJobsStartup
    {
        void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<DonorWorkflow>().Keyed<IWorkflow>(Workflow.Donor);
            builder.RegisterType<DoneeWorkflow>().Keyed<IWorkflow>(Workflow.Donee);

            builder.RegisterType<WorkflowFactory>().As<IWorkflowFactory>().SingleInstance();
        }

        public void Configure(IWebJobsBuilder job)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(Assembly.GetExecutingAssembly().GetCustomAttribute<NeutralResourcesLanguageAttribute>()!.CultureName);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;

            Log.Information(Strings.Startup.Starting);

            var builder = new ContainerBuilder();
            ConfigureContainer(builder);

            builder
                .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(t => t.GetInterfaces().Length > 0)
                .AsImplementedInterfaces();

            Log.Logger = new LoggerConfiguration()
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

            builder.RegisterInstance(Log.Logger).AsImplementedInterfaces();

            var container = builder.Build();

            job.AddExtension(new AutofacExtension(container));

            job.Services.AddLogging(lb => lb.AddSerilog(Log.Logger));
            job.Services.AddSingleton(Log.Logger);
            job.Services.AddSingleton(container);
            job.Services.AddApplicationInsightsTelemetry();
        }
    }
}

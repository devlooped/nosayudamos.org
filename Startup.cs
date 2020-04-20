using System;
using System.Diagnostics.Contracts;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Slack;

[assembly: FunctionsStartup(typeof(NosAyudamos.Startup))]

namespace NosAyudamos
{
    class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
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
                    }, restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: @"`{Category}:{Level}`
    ```{@Message:j}```"))
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Starting up");

            builder.Services.AddLogging(lb => lb.AddSerilog(logger));
            builder.Services.AddApplicationInsightsTelemetry();
            builder.Services.AddSingleton<IEnvironment, Environment>();
            builder.Services.AddSingleton<IMessaging, Messaging>();
            builder.Services.AddSingleton<ILanguageUnderstanding, LanguageUnderstanding>();
            builder.Services.AddSingleton<ITextAnalysis, TextAnalysis>();
            builder.Services.AddSingleton<IPersonRecognizer, PersonRecognizer>();
            builder.Services.AddSingleton<IBlobStorage, BlobStorage>();
            builder.Services.AddSingleton<IRepositoryFactory, RepositoryFactory>();
            builder.Services.AddSingleton<IWorkflowFactory, WorkflowFactory>();
            builder.Services.AddSingleton<IQRCode, QRCode>();
            builder.Services.AddTransient<IStartupWorkflow, StartupWorkflow>();
            builder.Services.AddTransient<IWorkflow, DonorWorkflow>();
            builder.Services.AddTransient<IWorkflow, DoneeWorkflow>();
        }
    }
}

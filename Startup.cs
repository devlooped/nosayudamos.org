using System;
using System.Diagnostics.Contracts;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Slack;

[assembly: FunctionsStartup(typeof(NosAyudamos.Startup))]

namespace NosAyudamos
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            Contract.Assert(builder != null);

            var logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Host", LogEventLevel.Warning)
                .MinimumLevel.Override("Function", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Azure", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Slack(new SlackSinkOptions
                {
                    WebHookUrl = Environment.GetEnvironmentVariable("SlackApiWebHook"),
                    CustomChannel = "#api",
                    ShowDefaultAttachments = false,
                    ShowPropertyAttachments = false,
                    ShowExceptionAttachments = true,
                }, restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: @"`{Category}:{Level}`
{Message}")
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Starting up");

            builder.Services.AddLogging(lb => lb.AddSerilog(logger));
            builder.Services.AddApplicationInsightsTelemetry();
            builder.Services.AddSingleton<IEnviroment, Enviroment>();
            builder.Services.AddSingleton<IMessaging, Messaging>();
            builder.Services.AddSingleton<ILanguageUnderstanding, LanguageUnderstanding>();
            builder.Services.AddSingleton<ITextAnalysis, TextAnalysis>();
            builder.Services.AddSingleton<IPersonRecognizer, PersonRecognizer>();
            builder.Services.AddSingleton<IBlobStorage, BlobStorage>();
        }
    }
}

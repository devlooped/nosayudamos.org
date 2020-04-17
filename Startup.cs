using System.Diagnostics.Contracts;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(NosAyudamos.Startup))]

namespace NosAyudamos
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            Contract.Assert(builder != null);

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
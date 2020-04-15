using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(NosAyudamos.Startup))]

namespace NosAyudamos
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IEnviroment, Enviroment>();
            builder.Services.AddSingleton<IMessaging, Messaging>();
            builder.Services.AddSingleton<ILanguageUnderstanding, LanguageUnderstanding>();
            builder.Services.AddSingleton<ITextAnalysis, TextAnalysis>();
        }
    }
}
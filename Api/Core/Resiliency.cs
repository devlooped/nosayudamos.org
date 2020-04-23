using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using Polly.Wrap;

namespace NosAyudamos
{
    class Resiliency
    {
        readonly IEnvironment environment;

        public Resiliency(IEnvironment environment) => this.environment = environment;

        public PolicyRegistry GetRegistry()
        {
            var registry = new PolicyRegistry();

            registry.Add<IAsyncPolicy<HttpResponseMessage>>(
                "HttpClientPolicy",
                HttpPolicyExtensions
                    .HandleTransientHttpError() // >= 500 || HttpStatusCode.RequestTimeout
                    .WaitAndRetryAsync(
                        environment.GetVariable<int>("ResilientNumberOfRetries"),
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            registry.Add(
                "LuisPolicy",
                Policy
                    .Handle<ErrorException>()
                    .WaitAndRetryAsync(
                        environment.GetVariable<int>("ResilientNumberOfRetries"),
                        retryAttempt => TimeSpan.FromSeconds(0.25 * Math.Pow(2, retryAttempt))));

            return registry;
        }
    }
}

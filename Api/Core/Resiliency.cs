using System;
using System.Net.Http;
using Azure;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using Twilio.Exceptions;

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
                        environment.GetVariable<int>("ResilientNumberOfRetries", 3),
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            registry.Add(
                "LuisPolicy",
                Policy
                    .Handle<ErrorException>()
                    .WaitAndRetryAsync(
                        environment.GetVariable<int>("ResilientNumberOfRetries", 3),
                        retryAttempt => TimeSpan.FromSeconds(0.25 * Math.Pow(2, retryAttempt))));

            registry.Add(
                "TextAnalysisPolicy",
                Policy
                    .Handle<RequestFailedException>()
                    .WaitAndRetryAsync(
                        environment.GetVariable<int>("ResilientNumberOfRetries", 3),
                        retryAttempt => TimeSpan.FromSeconds(0.25 * Math.Pow(2, retryAttempt))));

            registry.Add(
                "TwilioPolicy",
                Policy
                    .Handle<ApiConnectionException>()
                    .Or<ApiException>()
                    .WaitAndRetryAsync(
                        environment.GetVariable<int>("ResilientNumberOfRetries", 3),
                        retryAttempt => TimeSpan.FromSeconds(0.25 * Math.Pow(2, retryAttempt))));

            return registry;
        }
    }
}

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
        readonly IEnvironment env;

        public Resiliency(IEnvironment env) => this.env = env;

        public PolicyRegistry GetRegistry()
        {
            var registry = new PolicyRegistry();

            registry.Add<IAsyncPolicy<HttpResponseMessage>>(
                "HttpClientPolicy",
                HttpPolicyExtensions
                    .HandleTransientHttpError() // >= 500 || HttpStatusCode.RequestTimeout
                    .WaitAndRetryAsync(
                        env.GetVariable<int>("ResilientNumberOfRetries", 3),
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            registry.Add(
                "LuisPolicy",
                Policy
                    .Handle<ErrorException>()
                    .WaitAndRetryAsync(
                        env.GetVariable<int>("ResilientNumberOfRetries", 3),
                        retryAttempt => TimeSpan.FromSeconds(0.25 * Math.Pow(2, retryAttempt))));

            registry.Add(
                "TextAnalysisPolicy",
                Policy
                    .Handle<RequestFailedException>()
                    .WaitAndRetryAsync(
                        env.GetVariable<int>("ResilientNumberOfRetries", 3),
                        retryAttempt => TimeSpan.FromSeconds(0.25 * Math.Pow(2, retryAttempt))));

            registry.Add(
                "TwilioPolicy",
                Policy
                    .Handle<ApiConnectionException>()
                    .Or<ApiException>()
                    .WaitAndRetryAsync(
                        env.GetVariable<int>("ResilientNumberOfRetries", 3),
                        retryAttempt => TimeSpan.FromSeconds(0.25 * Math.Pow(2, retryAttempt))));

            return registry;
        }
    }
}

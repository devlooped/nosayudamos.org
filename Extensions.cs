using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Twilio.Security;

namespace NosAyudamos
{
    public static class Extensions
    {
        /// <summary>
        /// Gets whether the given request came from Twilio.
        /// </summary>
        public static bool IsTwilioRequest(this HttpRequest http)
        {
            Contract.Assert(http != null);
            return http.Headers.ContainsKey("X-Twilio-Signature");
        }

        /// <summary>
        /// Validates the Twilio signature of the request.
        /// </summary>
        public static bool IsTwilioSigned(this HttpRequest http)
        {
            Contract.Assert(http != null);
            if (!http.IsTwilioRequest())
                throw new ArgumentException("Request did not come from Twilio. Cannot verify its signature.", nameof(http));

            if (!http.Headers.TryGetValue("X-Twilio-Signature", out var values) ||
                values.Count != 1 ||
                string.IsNullOrEmpty(values[0]))
            {
                return false;
            }

            var signature = values[0];
            var token = Ensure.NotEmpty(Environment.GetEnvironmentVariable("TwilioAuthToken"), "TwilioAuthToken");

            if (signature == token)
                return true;

            var validator = new RequestValidator(token);
            var parameters = new Dictionary<string, string>();

            foreach (var parameter in http.Query)
            {
                parameters[parameter.Key] = parameter.Value;
            }

            var uri = new Uri(http.GetDisplayUrl());
            var url = uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped);

            return validator.Validate(url, parameters, signature);
        }
    }
}

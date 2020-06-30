using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Twilio.Security;

namespace NosAyudamos
{
    static class TwilioExtensions
    {
        /// <summary>
        /// Gets whether the given request came from Twilio.
        /// </summary>
        public static bool IsTwilioRequest(this HttpRequest http) => http.Headers.ContainsKey("X-Twilio-Signature");

        /// <summary>
        /// Validates the Twilio signature of the request.
        /// </summary>
        public static bool IsTwilioSigned(this HttpRequest http, IEnvironment environment, string body)
        {
            if (!http.IsTwilioRequest())
                throw new ArgumentException("Request did not come from Twilio. Cannot verify its signature.", nameof(http));

            if (!http.Headers.TryGetValue("X-Twilio-Signature", out var values) ||
                values.Count != 1 ||
                string.IsNullOrEmpty(values[0]))
            {
                return false;
            }

            var signature = values[0];
            var token = environment.GetVariable("TwilioAuthToken");

            if (signature == token)
                return true;

            var validator = new RequestValidator(token);
            var parameters = new Dictionary<string, string>();

            foreach (var parameter in body.Split('&').Select(x => x.Split('=')))
            {
                parameters[parameter[0]] = WebUtility.UrlDecode(parameter[1]);
            }

            var uri = new Uri(http.GetDisplayUrl());
            var url = uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped);

            return validator.Validate(url, parameters, signature) ||
                validator.Validate(url.Replace("http://", "https://", StringComparison.Ordinal), parameters, signature);
        }
    }
}

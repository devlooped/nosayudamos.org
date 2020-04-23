using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    class ChatApi
    {
        readonly string chatApiNumber;
        readonly HttpClient httpClient;
        readonly ILogger<ChatApi> logger;

        public ChatApi(IEnvironment enviroment, HttpClient httpClient, ILogger<ChatApi> logger)
        {
            chatApiNumber = enviroment.GetVariable("ChatApiNumber");
            this.httpClient = httpClient;
            this.logger = logger;
        }

        [FunctionName("chat")]
        public async Task<IActionResult> EncodeAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            Contract.Assert(req != null);

            var uri = new Uri(req.GetDisplayUrl());
            var formatter = new JsonMediaTypeFormatter();

            using var reader = new StreamReader(req.Body);
            var payload = await reader.ReadToEndAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(payload);
            var responses = new List<string>();

            foreach (var message in json.GetProperty("messages").EnumerateArray())
            {
                var body = message.GetProperty("body").GetString();
                var from = message.GetProperty("author").GetString();
                var at = from.IndexOf('@', StringComparison.Ordinal);
                if (at != -1)
                    from = from.Substring(0, at);

                from = "+" + from.TrimStart('+');

                // Avoid reentrancy from our own messages.
                if (from == chatApiNumber)
                    continue;

                using var content = new StringContent(WebUtility.UrlEncode($"From=+{from.TrimStart('+')}&To={chatApiNumber}&Body={body}"));
                using var response = await httpClient.PostAsync(new Uri(uri, "whatsapp"), content);

                responses.Add(await response.Content.ReadAsStringAsync());
            }

            return new OkObjectResult(responses.ToArray());
        }
    }
}

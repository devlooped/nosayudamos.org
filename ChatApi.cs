using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
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
    public class ChatApi
    {
        readonly ILogger<ChatApi> logger;

        public ChatApi(ILogger<ChatApi> logger) => this.logger = logger;

        [FunctionName("chat")]
        public async Task<IActionResult> EncodeAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            Contract.Assert(req != null);

            var uri = new Uri(req.GetDisplayUrl());
            using var http = new HttpClient();
            var formatter = new JsonMediaTypeFormatter();

            using var reader = new StreamReader(req.Body);
            var payload = await reader.ReadToEndAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(payload);
            var responses = new List<string>();

            logger.Log(LogLevel.Information, responses.ToArray());

            foreach (var message in json.GetProperty("messages").EnumerateArray())
            {
                var body = message.GetProperty("body").GetString();
                var from = message.GetProperty("author").GetString();
                var at = from.IndexOf('@', StringComparison.Ordinal);
                if (at != -1)
                    from = from.Substring(0, at);

                using var response = await http.PostAsync(new Uri(uri, "whatsapp"), new { from, body }, formatter);
                responses.Add(await response.Content.ReadAsStringAsync());
            }

            return new OkObjectResult(responses.ToArray());
        }
    }
}

﻿using System;
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

namespace NosAyudamos
{
    public class ChatApi
    {
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

            foreach (var message in json.GetProperty("messages").EnumerateArray())
            {
                var body = message.GetProperty("body").GetString();
                var from = message.GetProperty("author").GetString();
                var at = from.IndexOf('@', StringComparison.Ordinal);
                if (at != -1)
                    from = from.Substring(0, at);

                _ = await http.PostAsync(new Uri(uri, "whatsapp"), new { from, body }, formatter);
            }

            return new OkObjectResult("");
        }
    }
}

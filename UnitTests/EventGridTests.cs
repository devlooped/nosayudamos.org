using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.AspNetCore.Testing;

namespace NosAyudamos
{
    public class EventGridTests
    {
        Uri gridUri;
        string gridKey;

        public EventGridTests()
        {
            if (File.Exists("local.settings.json"))
            {
                dynamic settings = JObject.Parse(File.ReadAllText("local.settings.json"));
                gridUri = new Uri((string)settings.Values.EventGridUrl);
                gridKey = settings.Values.EventGridAccessKey;
            }
        }

        [SkipOnCI]
        [ConditionalFact]
        public async Task SendCustomEvent()
        {
            var domain = gridUri.Host;
            var credentials = new TopicCredentials(gridKey);
            using var client = new EventGridClient(credentials);

            await client.PublishEventsAsync(domain, new List<EventGridEvent>
            {
                new EventGridEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Topic = "NosAyudamos.Inbox",
                    EventType = "NosAyudamos.Messaging",
                    EventTime = DateTime.Now,
                    Subject = "23696294",
                    Data = "{ \"body\": \"ayuda\" }",
                    DataVersion = "1.0",
                }
            });
        }
    }
}

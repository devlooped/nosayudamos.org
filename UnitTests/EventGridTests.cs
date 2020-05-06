using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;

namespace NosAyudamos
{
    public class EventGridTests
    {
        // NOTE: you can run this method with the ad-hoc TestDriven runner.
        public async Task SendCustomEvent()
        {
            var env = new Environment();
            var domain = new Uri(env.GetVariable("EventGridUrl")).Host;
            var credentials = new TopicCredentials(env.GetVariable("EventGridAccessKey"));
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

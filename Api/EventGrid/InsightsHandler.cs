using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.EventGrid
{
    class TelemetryHandler
    {
        readonly IEnvironment env;
        readonly TelemetryClient client;

        public TelemetryHandler(IEnvironment env, TelemetryClient client)
            => (this.env, this.client)
            = (env, client);

        [FunctionName("insights")]
        public Task RunAsync([EventGridTrigger] EventGridEvent e)
        {
            if (env.IsDevelopment() || !(e.Data is string json))
                return Task.CompletedTask;

            var data = (JObject?)JsonConvert.DeserializeObject(json);
            if (data == null)
                return Task.CompletedTask;

            var ev = new EventTelemetry(e.GetType().FullName);

            foreach (var prop in data.Properties().Where(prop =>
                // Get only primitive values
                prop.Value.Type != JTokenType.Array &&
                prop.Value.Type != JTokenType.Object &&
                prop.Value.Type != JTokenType.Null))
            {
                ev.Properties[prop.Name] = prop.Value.ToString();
            }

            ev.Properties["EventId"] = e.Id;
            ev.Properties[nameof(EventGridEvent.Subject)] = e.Subject;
            ev.Properties[nameof(EventGridEvent.Topic)] = e.Topic;

            // Record the time it took from app-generated EventTime (when we 
            // pushed the event to the grid) to the current time, meassured 
            // in milliseconds since that's enough for detecting unusual spikes)
            var eventTicks = e.EventTime.ToUniversalTime().Ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond;
            var nowTicks = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond;

            ev.Metrics.Add("EventGridDelay", (nowTicks - eventTicks) / TimeSpan.TicksPerMillisecond);

            client.TrackEvent(ev);

            return Task.CompletedTask;
        }
    }
}

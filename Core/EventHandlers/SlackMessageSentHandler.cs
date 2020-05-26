using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos
{
    class PhoneThread
    {
        public PhoneThread(string phoneNumber, string threadId, DateTime lastUpdated)
            => (PhoneNumber, ThreadId, LastUpdated)
            = (phoneNumber, threadId, lastUpdated);

        [Key]
        public string PhoneNumber { get; }
        public string ThreadId { get; }
        public DateTime LastUpdated { get; }
    }

    class SlackMessageSentHandler : IEventHandler<SlackMessageSent>
    {
        readonly IEnvironment environment;
        readonly IEntityRepository<PhoneThread> repository;
        readonly HttpClient http;

        public SlackMessageSentHandler(IEnvironment environment, IEntityRepository<PhoneThread> repository, HttpClient http)
            => (this.environment, this.repository, this.http)
            = (environment, repository, http);

        public async Task HandleAsync(SlackMessageSent e)
        {
            if (environment.IsDevelopment() && !environment.GetVariable("SendToSlackInDevelopment", false))
                return;

            var payload = JObject.Parse(e.MessageJson);
            if (payload.Property("channel", StringComparison.OrdinalIgnoreCase) == null)
            {
                payload["channel"] = environment.IsDevelopment() ?
                    environment.GetVariable("SlackTestChannel") :
                    environment.GetVariable("SlackUsersChannel");
            }

            string? threadId = default;
            bool? broadcast = default;
            var thread = await repository.GetAsync(e.PhoneNumber);

            if (thread != null)
            {
                if ((DateTime.Now - thread.LastUpdated).Days < 30)
                    threadId = thread.ThreadId;

                if ((DateTime.Now - thread.LastUpdated).Days > 7)
                    broadcast = true;
            }

            if (broadcast != null)
                payload["reply_broadcast"] = broadcast.Value;

            if (threadId != null)
            {
                payload["thread_ts"] = threadId;
            }
            else
            {
                // Ensure thread header always has the sender phone block
                if (payload.SelectToken("$.blocks[?(@.block_id == 'sender')]") == null)
                {
                    if (payload["blocks"] == null)
                    {
                        payload["blocks"] = new JArray
                        {
                            new JObject
                            {
                                { "type", "divider" },
                            },
                            new JObject
                            {
                                { "block_id", "body" },
                                { "type", "section" },
                                { "text", new JObject
                                    {
                                        { "type", "plain_text" },
                                        { "text", payload["text"] },
                                        { "emoji", true },
                                    }
                                }
                            }
                        };
                    }

                    payload["blocks"]!.First!.AddAfterSelf(new JObject
                    {
                        { "block_id", "sender" },
                        { "type", "section" },
                        {
                            "fields", new JArray
                            {
                                new JObject
                                {
                                    { "type", "plain_text" },
                                    { "text", ":message: " + e.PhoneNumber },
                                    { "emoji", true },
                                }
                            }
                        }
                    });
                }

                // Ensure top of thread actions 
                var actions = payload.SelectToken("$.blocks[?(@.type == 'actions')]");
                if (actions == null)
                {
                    actions = new JObject
                    {
                        { "block_id", "actions" },
                        { "type", "actions" },
                        { "elements", new JArray() }
                    };
                    ((JArray)payload["blocks"]!).Add(actions);
                }

                var elements = (JArray)actions["elements"]!;

                // Ensure 'pause' action
                if (payload.SelectToken("$.blocks[?(@.type == 'actions')].elements[?(@.value == 'pause')]") == null)
                {
                    elements.Add(new JObject
                    {
                        { "type", "button" },
                        { "text", new JObject
                            {
                                { "type", "plain_text" },
                                { "text", "Pause :automation_pause:" },
                                { "emoji", true },
                            }
                        },
                        { "value", "pause" },
                    });
                }

                // Ensure 'resume' action
                if (payload.SelectToken("$.blocks[?(@.type == 'actions')].elements[?(@.value == 'resume')]") == null)
                {
                    elements.Add(new JObject
                    {
                        { "type", "button" },
                        { "text", new JObject
                            {
                                { "type", "plain_text" },
                                { "text", "Resume :automation_resume:" },
                                { "emoji", true },
                            }
                        },
                        { "value", "resume" }
                    });
                }
            }

            using var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage")
            {
                Content = content
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", environment.GetVariable("SlackToken"));
            var response = await http.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            if ((bool?)json["ok"] == false)
            {
                throw new HttpRequestException((string?)json["error"] ?? json.ToString());
            }

            if (threadId == null)
                threadId = json["ts"]?.ToString();

            if (threadId != null)
                await repository.PutAsync(new PhoneThread(e.PhoneNumber, threadId, DateTime.Now));
        }
    }
}

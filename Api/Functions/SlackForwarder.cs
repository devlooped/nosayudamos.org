using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NosAyudamos.Events;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using SlackBotMessages;
using System.Collections.Generic;
using SlackBotMessages.Models;
using SlackMessage = SlackBotMessages.Models.Message;

namespace NosAyudamos.Functions
{
    class SlackForwarder : IEventHandler<DeadMessageReceived>
    {
        readonly ISerializer serializer;
        readonly IEnvironment environment;
        readonly IPersonRepository repository;
        readonly ILogger<Slack> logger;

        public SlackForwarder(ISerializer serializer, IEnvironment environment, IPersonRepository repository, ILogger<Slack> logger) =>
            (this.serializer, this.environment, this.repository, this.logger) = (serializer, environment, repository, logger);

        [FunctionName("slack_forward")]
        public async Task ForwardAsync([EventGridTrigger] EventGridEvent e) =>
            // TODO: validate Topic, Subject, EventType
            await HandleAsync(serializer.Deserialize<DeadMessageReceived>(e.Data));

        public async Task HandleAsync(DeadMessageReceived e)
        {
            if (environment.IsDevelopment())
                return;

            var client = new SbmClient(environment.GetVariable("SlackUsersWebHook"));
            var payload = @$"From:{e.From}
Body:{e.Body}
To:{e.To}";

            var from = "Unknown";
            if (e.PersonId != null)
            {
                var person = await repository.GetAsync(e.PersonId);
                from = person.FirstName + " " + person.LastName;
            }

            var message = new SlackMessage
            {
                Username = "nosayudamos",
                Attachments = new List<Attachment>
                {
                    new Attachment
                    {
                        //Pretext = Emoji.GreyQuestion + " Unprocessed message",
                        Color = "warning",
                        Fields = new List<Field>
                        {
                            new Field { Title = "From", Value = e.From, Short = true },
                            new Field { Title = "To", Value = e.To, Short = true },
                            new Field
                            {
                                Title = "Body",
                                Value = e.Body,
                            }
                        },
                        Fallback = payload,
                    }.SetFooter(from, null, e.When.DateTime)
                }
            };

            await client.SendAsync(message).ConfigureAwait(false);
        }
    }
}

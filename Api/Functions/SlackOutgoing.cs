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
    class SlackOutgoing : IEventHandler<UnknownMessageReceived>
    {
        readonly ISerializer serializer;
        readonly IEnvironment environment;
        readonly IPersonRepository repository;
        readonly ILogger<SlackIncoming> logger;

        public SlackOutgoing(ISerializer serializer, IEnvironment environment, IPersonRepository repository, ILogger<SlackIncoming> logger) =>
            (this.serializer, this.environment, this.repository, this.logger) = (serializer, environment, repository, logger);

        [FunctionName("slack_outgoing")]
        public Task SendAsync([EventGridTrigger] EventGridEvent e) => HandleAsync(e.GetData<UnknownMessageReceived>(serializer));

        public async Task HandleAsync(UnknownMessageReceived e)
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

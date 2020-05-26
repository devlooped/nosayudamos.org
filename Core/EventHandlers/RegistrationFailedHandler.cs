using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Slack.Webhooks;
using Slack.Webhooks.Blocks;
using Slack.Webhooks.Elements;
using Slack.Webhooks.Interfaces;

namespace NosAyudamos
{
    class RegistrationFailedHandler : IEventHandler<RegistrationFailed>
    {
        readonly IEventStreamAsync events;

        public RegistrationFailedHandler(IEventStreamAsync events) => this.events = events;

        public async Task HandleAsync(RegistrationFailed e)
        {
            var message = new SlackMessage
            {
                Username = "nosayudamos",
                Blocks = new List<Block>
                {
                    new Divider(),
                    new Section
                    {
                        BlockId = "sender",
                        Fields = new List<TextObject>
                        {
                            new TextObject($":zap: {e.PhoneNumber}") { Emoji = true },
                            new TextObject($"Registration failed :interrobang:") { Emoji = true },
                        }
                    },
                },
            };

            message.Blocks.AddRange(e.Images.Select((uri, i) => new global::Slack.Webhooks.Blocks.Image
            {
                ImageUrl = uri.OriginalString,
                Title = new TextObject($"Attempt #" + i),
                AltText = Path.GetFileName(uri.AbsolutePath),
            }));

            message.Blocks.Add(new Actions
            {
                BlockId = "actions",
                Elements = new List<IActionElement>
                {
                    new Button
                    {
                        Text = new TextObject("Register :register_donee:") { Emoji = true },
                        Value = "register"
                    },
                }
            });

            await events.PushAsync(new SlackMessageSent(e.PhoneNumber, message.AsJson()));
        }
    }
}

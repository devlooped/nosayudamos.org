using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Slack.Webhooks;
using Slack.Webhooks.Blocks;
using Slack.Webhooks.Elements;
using Slack.Webhooks.Interfaces;

namespace NosAyudamos
{
    class SlackUnknownMessageReceivedHandler : IEventHandler<UnknownMessageReceived>
    {
        readonly IEnvironment environment;
        readonly IPersonRepository repository;
        readonly ILanguageUnderstanding language;
        readonly HttpClient http;

        public SlackUnknownMessageReceivedHandler(IEnvironment environment, IPersonRepository repository, ILanguageUnderstanding language, HttpClient http)
            => (this.environment, this.repository, this.language, this.http)
            = (environment, repository, language, http);

        public async Task HandleAsync(UnknownMessageReceived e)
        {
            if (environment.IsDevelopment() && !environment.GetVariable("SendToSlackInDevelopment", false))
                return;

            var url = environment.GetVariable("SlackUsersWebHook");
            var from = "Unknown";
            if (e.PersonId != null &&
                await repository.GetAsync(e.PersonId) is Person person &&
                person != null)
            {
                from = person.FirstName + " " + person.LastName;
            }

            var intents = await language.GetIntentsAsync(e.Body);
            var context = new StringBuilder();
            if (intents.TryGetValue("Help", out var help))
                context = context.Append(":pray: ").Append(help.Score?.ToString("0.##", CultureInfo.CurrentCulture));

            if (intents.TryGetValue("Donate", out var donate))
                context = context.Append(":money_with_wings: ").Append(donate.Score?.ToString("0.##", CultureInfo.CurrentCulture));

            context = context.Append(" by ").Append(from).Append(", ").Append(e.When.Humanize());
            var toEmoji = e.To == environment.GetVariable("ChatApiNumber").TrimStart('+') ? ":whatsapp:" : ":twilio:";

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
                            new TextObject($":point_right: {e.From}") { Emoji = true },
                            new TextObject($"{toEmoji} {e.To}") { Emoji = true },
                        }
                    },
                    new Section
                    {
                        BlockId = "body",
                        Text = new TextObject($"> {e.Body}") { Type = TextObject.TextType.Markdown, Emoji = true },
                    },
                    new Context
                    {
                        Elements = new List<IContextElement>
                        {
                            new TextObject(context.ToString().Trim()) { Emoji = true },
                        }
                    },
                    new Actions
                    {
                        Elements = new List<IActionElement>
                        {
                            new Button
                            {
                                Text = new TextObject("Train as :pray:") { Emoji = true },
                                Style = "primary",
                                Value = "help"
                            },
                            new Button
                            {
                                Text = new TextObject("Train as :money_with_wings:") { Emoji = true },
                                Value = "donate"
                            },
                            new Button
                            {
                                Text = new TextObject("Retry :repeat:") { Emoji = true },
                                Value = "retry"
                            },
                        }
                    }
                },
            };

            using var content = new StringContent(message.AsJson(), Encoding.UTF8, "application/json");
            await http.PostAsync(url, content);
        }
    }
}

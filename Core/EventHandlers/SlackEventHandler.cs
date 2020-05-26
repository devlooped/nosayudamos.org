using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Newtonsoft.Json;
using Slack.Webhooks;
using Slack.Webhooks.Blocks;
using Slack.Webhooks.Elements;
using Slack.Webhooks.Interfaces;

namespace NosAyudamos
{
    class SlackEventHandler :
        IEventHandler<TaxStatusRejected>,
        IEventHandler<TaxStatusAccepted>,
        IEventHandler<UnknownMessageReceived>,
        IEventHandler<MessageReceived>
    {
        readonly IEnvironment environment;
        readonly IPersonRepository repository;
        readonly IEntityRepository<PhoneSystem> phoneRepo;
        readonly IEventStreamAsync events;
        readonly ILanguageUnderstanding language;

        public SlackEventHandler(
            IEnvironment environment, IPersonRepository repository,
            IEntityRepository<PhoneSystem> phoneRepo, IEventStreamAsync events,
            ILanguageUnderstanding language)
            => (this.environment, this.repository, this.phoneRepo, this.events, this.language)
            = (environment, repository, phoneRepo, events, language);

        public async Task HandleAsync(UnknownMessageReceived e)
        {
            if (environment.IsDevelopment() && !environment.GetVariable("SendToSlackInDevelopment", false))
                return;

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
            var toEmoji = e.PhoneNumber == environment.GetVariable("ChatApiNumber").TrimStart('+') ? ":whatsapp:" : ":twilio:";
            var map = await phoneRepo.GetAsync(e.PhoneNumber);

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
                            new TextObject($":unknown: {e.PhoneNumber}") { Emoji = true },
                            new TextObject($"{toEmoji} {map?.SystemNumber}") { Emoji = true },
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
                        BlockId = "actions",
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
                            new Button
                            {
                                Text = new TextObject("Pause :automation_pause:") { Emoji = true },
                                Value = "pause"
                            },
                            new Button
                            {
                                Text = new TextObject("Resume :automation_resume:") { Emoji = true },
                                Value = "resume"
                            },
                        }
                    }
                },
            };

            await events.PushAsync(new SlackMessageSent(e.PhoneNumber, message.AsJson())).ConfigureAwait(false);
        }

        public async Task HandleAsync(TaxStatusAccepted e)
        {
            var person = await repository.GetAsync(e.SourceId!);
            if (person == null)
                return;

            await events.PushAsync(new MessageSent(
                person.PhoneNumber,
                Strings.UI.Donee.Welcome(person.FirstName.Split(' ').First(), person.Sex == Sex.Male ? "o" : "a")))
                .ConfigureAwait(false);

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
                            new TextObject($":approved: 5491159278282") { Emoji = true },
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age}), <https://www.cuitonline.com/search.php?q={e.SourceId}|{e.TaxId}>")
                            {
                                Type = TextObject.TextType.Markdown
                            },
                        }
                    },
                    new Actions
                    {
                        BlockId = "actions",
                        Elements = new List<IActionElement>
                        {
                            new Button
                            {
                                Text = new TextObject("Pause :automation_pause:") { Emoji = true },
                                Value = "pause"
                            },
                            new Button
                            {
                                Text = new TextObject("Resume :automation_resume:") { Emoji = true },
                                Value = "resume"
                            },
                        }
                    }
                },
            };

            await events.PushAsync(new SlackMessageSent(person.PhoneNumber, message.AsJson()));
        }

        public async Task HandleAsync(TaxStatusRejected e)
        {
            var person = await repository.GetAsync(e.SourceId!);
            if (person == null)
                return;

            var name = person.FirstName.Split(' ').First();
            var body = e.Reason switch
            {
                TaxStatusRejectedReason.NotApplicable => Strings.UI.Donee.NotApplicable(name),
                TaxStatusRejectedReason.HasIncomeTax => Strings.UI.Donee.HasIncomeTax(name),
                TaxStatusRejectedReason.HighCategory => Strings.UI.Donee.HighCategory(name),
                _ => Strings.UI.Donee.Rejected,
            };

            await events.PushAsync(new MessageSent(person.PhoneNumber, body));

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
                            new TextObject($":rejected: 5491159278282") { Emoji = true },
                            new TextObject($"{person.FirstName} {person.LastName} ({person.Age}), <https://www.cuitonline.com/search.php?q={e.SourceId}|{e.TaxId}>")
                            {
                                Type = TextObject.TextType.Markdown
                            },
                        }
                    },
                    new Actions
                    {
                        BlockId = "actions",
                        Elements = new List<IActionElement>
                        {
                            new Button
                            {
                                Text = new TextObject("Pause :automation_pause:") { Emoji = true },
                                Value = "pause"
                            },
                            new Button
                            {
                                Text = new TextObject("Resume :automation_resume:") { Emoji = true },
                                Value = "resume"
                            },
                        }
                    }
                },
            };

            await events.PushAsync(new SlackMessageSent(person.PhoneNumber, message.AsJson()));
        }

        /// <summary>
        /// When automation is disabled for the user, we forward all messages to slack
        /// so interaction can be taken over manually.
        /// </summary>
        public async Task HandleAsync(MessageReceived e)
        {
            var map = await phoneRepo.GetAsync(e.PhoneNumber);
            // We only foward for paused phones.
            if (map == null || map.AutomationPaused != true)
                return;

            await events.PushAsync(new SlackMessageSent(
                e.PhoneNumber,
                JsonConvert.SerializeObject(new
                {
                    text = e.Body
                })))
                .ConfigureAwait(false);
        }
    }
}

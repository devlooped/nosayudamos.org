using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    // This workflow is only selected if there is no person registered yet,
    // since its [Workflow] attribute does not specify a role.
    [Workflow]
    class StartupWorkflow : IWorkflow
    {
        readonly IEventStreamAsync events;
        readonly IEntityRepository<PhoneEntry> phoneDir;
        readonly IWorkflow doneeWorkflow;

        public StartupWorkflow(
            IEventStreamAsync events,
            IEntityRepository<PhoneEntry> phoneDir,
            IWorkflowSelector selector)
            => (this.events, this.phoneDir, doneeWorkflow)
            = (events, phoneDir, selector.Select(Role.Donee));

        public async Task RunAsync(PhoneEntry phone, MessageReceived message, TextAnalysis analysis, Person? person)
        {
            // If we receive an image from an unregistered number with no previously 
            // set Role, we attempt to register as a donee that's sending their ID
            // TODO: ignore moderated content
            if (Uri.TryCreate(message.Body, UriKind.Absolute, out _))
            {
                await doneeWorkflow.RunAsync(phone, message, analysis, person);
                return;
            }

            if (analysis.Prediction.IsIntent(Intents.Utilities.Help, Intents.Help))
            {
                phone.Role = Role.Donee;
                await phoneDir.PutAsync(phone);
                // User wants to be a donee, we need the ID
                await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.Donee.SendIdentifier)).ConfigureAwait(false);
            }
            else if (analysis.Prediction.IsIntent(Intents.Donate))
            {
                phone.Role = Role.Donor;
                await phoneDir.PutAsync(phone);
                await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.Donor.SendAmount)).ConfigureAwait(false);
            }
            else
            {
                // Can't figure out intent, or score is too low.
                await events.PushAsync(new UnknownMessageReceived(message.PhoneNumber, message.Body) { When = message.When }).ConfigureAwait(false);
                await events.PushAsync(new MessageSent(message.PhoneNumber, Strings.UI.UnknownIntent)).ConfigureAwait(false);
            }
        }
    }
}

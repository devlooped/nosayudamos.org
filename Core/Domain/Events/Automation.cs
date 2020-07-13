using System.Threading.Tasks;

namespace NosAyudamos
{
    /// <summary>
    /// Event triggered when the automated processing of incoming 
    /// messages from a given phone number is paused to allow for 
    /// manual intervention by the team.
    /// </summary>
    public class AutomationPaused
    {
        public AutomationPaused(string phoneNumber, string by)
            => (PhoneNumber, By)
            = (phoneNumber, by);

        /// <summary>
        /// The team member that paused automation for the given 
        /// phone number.
        /// </summary>
        public string By { get; }
        /// <summary>
        /// The phone number for which incoming message processing 
        /// automation will be disabled.
        /// </summary>
        public string PhoneNumber { get; }
    }

    /// <summary>
    /// Event triggered when the automated processing of incoming 
    /// messages from a given phone number is resumed.
    /// </summary>
    public class AutomationResumed
    {
        public AutomationResumed(string phoneNumber, string by)
            => (PhoneNumber, By)
            = (phoneNumber, by);

        /// <summary>
        /// The team member that resumed automation for the given 
        /// phone number.
        /// </summary>
        public string By { get; }
        /// <summary>
        /// The phone number for which incoming message processing 
        /// automation has been resumed..
        /// </summary>
        public string PhoneNumber { get; }
    }

    class AutomationEventsHandler : IEventHandler<AutomationPaused>, IEventHandler<AutomationResumed>
    {
        readonly IEntityRepository<PhoneEntry> phoneDir;

        public AutomationEventsHandler(IEntityRepository<PhoneEntry> phoneDir) => this.phoneDir = phoneDir;

        public async Task HandleAsync(AutomationResumed e)
        {
            var phone = await phoneDir.GetAsync(e.PhoneNumber);
            if (phone != null && phone.AutomationPaused == true)
            {
                phone.AutomationPaused = false;
                await phoneDir.PutAsync(phone);
            }
        }

        public async Task HandleAsync(AutomationPaused e)
        {
            var phone = await phoneDir.GetAsync(e.PhoneNumber);
            if (phone == null)
                phone = new PhoneEntry(e.PhoneNumber, "");

            if (phone.AutomationPaused != true)
            {
                phone.AutomationPaused = true;
                await phoneDir.PutAsync(phone);
            }
        }
    }
}

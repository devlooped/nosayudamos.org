using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Slack
{
    /// <summary>
    /// Represents a processor for Slack callback payloads received from 
    /// actions performed in Slack, such as replying to a message in a thread 
    /// with a user, or filling out forms or triggering actions from buttons 
    /// in messages' UIs.
    /// </summary>
    interface ISlackPayloadProcessor
    {
        /// <summary>
        /// Determines whether the processor can act on the given Slack payload JSON.
        /// </summary>
        bool AppliesTo(JObject payload);
        /// <summary>
        /// Processes the payload by typically generating further domain events.
        /// </summary>
        Task ProcessAsync(JObject payload);
    }
}

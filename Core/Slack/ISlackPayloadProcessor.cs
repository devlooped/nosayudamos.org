using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos.Slack
{
    interface ISlackPayloadProcessor
    {
        bool AppliesTo(JObject payload);
        Task ProcessAsync(JObject payload);
    }
}

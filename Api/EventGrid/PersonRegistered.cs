using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace NosAyudamos.EventGrid
{
    class PersonRegistered
    {
        readonly ValidateTaxStatusHandler handler;
        readonly ISerializer serializer;

        public PersonRegistered(ValidateTaxStatusHandler handler, ISerializer serializer)
            => (this.handler, this.serializer)
            = (handler, serializer);

        [FunctionName("person-registered")]
        public Task RunAsync([EventGridTrigger] EventGridEvent e) => handler.HandleAsync(e.GetData<NosAyudamos.PersonRegistered>(serializer));
    }
}

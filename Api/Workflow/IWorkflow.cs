using System.Threading.Tasks;
using NosAyudamos.Events;

namespace NosAyudamos
{
    interface IWorkflow
    {
        Task RunAsync(MessageEvent @event);
    }

    enum Workflow
    {
        Donor,
        Donee
    }
}

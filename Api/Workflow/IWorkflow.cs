using System.Threading.Tasks;

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

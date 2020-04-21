using System.Threading.Tasks;

namespace NosAyudamos
{
    interface IWorkflow
    {
        Task RunAsync(Message message);
    }

    enum Workflow
    {
        Donor,
        Donee
    }
}

using System.Threading.Tasks;

namespace NosAyudamos
{
    interface IWorkflow
    {
        Task RunAsync(MessageReceived message, TextAnalysis analysis, Person? person);
    }
}

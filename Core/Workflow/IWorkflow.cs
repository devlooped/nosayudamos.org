using System.Threading.Tasks;

namespace NosAyudamos
{
    interface IWorkflow
    {
        Task RunAsync(PhoneEntry phone, MessageReceived message, TextAnalysis analysis, Person? person);
    }
}

using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;

namespace NosAyudamos
{
    interface IWorkflow
    {
        Task RunAsync(MessageReceived message, Prediction prediction, Person? person);
    }
}

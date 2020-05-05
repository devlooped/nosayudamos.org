using System.Threading.Tasks;
using Merq;

namespace NosAyudamos
{
    interface IEventStreamAsync : IEventStream
    {
        /// <summary>
        /// Pushes an event to the stream, causing any subscriber to be invoked if appropriate.
        /// </summary>
        Task PushAsync<TEvent>(TEvent args);
    }
}

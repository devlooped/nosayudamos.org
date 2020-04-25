using System.Threading.Tasks;

namespace NosAyudamos
{
    /// <summary>
    /// Interface implemented by functions that process events from 
    /// Event Grid, after converting them to domain model events, 
    /// for the purposes of handling them and also be testable.
    /// </summary>
    /// <typeparam name="TEvent">Type of event handled by the component.</typeparam>
    interface IEventHandler<TEvent>
    {
        /// <summary>
        /// Handles the event.
        /// </summary>
        Task HandleAsync(TEvent e);
    }
}

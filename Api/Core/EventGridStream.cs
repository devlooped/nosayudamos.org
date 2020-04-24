using System.Composition;
using Merq;

namespace NosAyudamos
{
    [Shared]
    class EventGridStream : EventStream
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0022:Use expression body for methods", Justification = "WIP")]
        public override void Push<TEvent>(TEvent @event)
        {
            base.Push(@event);
            // TODO: convert to EventGrid model and push it.
        }
    }
}

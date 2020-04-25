using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Merq;

namespace NosAyudamos
{
    class FeatureEventStream : EventStream
    {
        readonly IContainer container;

        public FeatureEventStream(IContainer container) => this.container = container;

        public override void Push<TEvent>(TEvent @event)
        {
            base.Push(@event);

            if (container.TryResolve<IEnumerable<IEventHandler<TEvent>>>(out var handlers))
            {
                Task.WaitAll(handlers.Select(x => x.HandleAsync(@event)).ToArray(), 10000);
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Merq;
using Serilog;

namespace NosAyudamos
{
    class FeatureEventStream : EventStream
    {
        readonly IContainer container;
        readonly ILogger logger;

        public FeatureEventStream(IContainer container, ILogger logger)
            => (this.container, this.logger) = (container, logger);

        public override void Push<TEvent>(TEvent @event)
        {
            base.Push(@event);

            if (container.TryResolve<IEnumerable<IEventHandler<TEvent>>>(out var handlers))
            {
                Task.WaitAll(handlers.Select(x => x.HandleAsync(@event)).ToArray());
            }
        }
    }
}

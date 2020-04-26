using System.Collections.Generic;
using System.Linq;
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
                foreach (var handler in handlers)
                {
                    logger.Verbose(@"Invoking {@handler:j} with {@event:j}", handler, @event);
                    handler.HandleAsync(@event).Wait();
                }
            }
        }
    }
}

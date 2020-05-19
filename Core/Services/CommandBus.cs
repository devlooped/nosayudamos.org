using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Composition;

namespace Merq
{
    [Shared]
    internal class CommandBusComponent : ICommandBus
    {
        MethodInfo canHandleMethod = typeof(CommandBusComponent)
            .GetTypeInfo()
            .GetDeclaredMethods("CanHandle")
            .First(m => m.IsGenericMethodDefinition);

        IServiceProvider services;
        Runner forCommands;

        public CommandBusComponent(IServiceProvider services)
        {
            this.services = services;
            forCommands = new Runner(services);
        }

        public bool CanExecute<TCommand>(TCommand command) where TCommand : IExecutable
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var handler = (ICanExecute<TCommand>)services.GetService(typeof(ICanExecute<TCommand>));

            return handler == null ? false : handler.CanExecute(command);
        }

        public bool CanHandle(IExecutable command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            try
            {
                return (bool)canHandleMethod.MakeGenericMethod(command.GetType())
                    .Invoke(this, Array.Empty<object>());
            }
            catch (TargetInvocationException ex)
            {
                // Rethrow the inner exception preserving stack trace.
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                // Will never get here.
                throw ex.InnerException;
            }
        }

        public bool CanHandle<TCommand>() where TCommand : IExecutable 
            => services.GetService(typeof(ICanExecute<TCommand>)) != null;

        public void Execute(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            ForCommand().Execute((dynamic)command);
        }

        public TResult Execute<TResult>(ICommand<TResult> command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            return ForResult<TResult>().Execute((dynamic)command);
        }

        public Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            return ForCommand().ExecuteAsync((dynamic)command, cancellation);
        }

        public Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            return ForResult<TResult>().ExecuteAsync((dynamic)command, cancellation);
        }

        Runner ForCommand() => forCommands;

        Runner<TResult> ForResult<TResult>() => new Runner<TResult>(services);

        class Runner
        {
            IServiceProvider services;

            public Runner(IServiceProvider services) => this.services = services;

            public void Execute<TCommand>(TCommand command) where TCommand : ICommand
            {
                var handler = (ICommandHandler<TCommand>)services.GetService(typeof(ICommandHandler<TCommand>));
                if (handler == null)
                    throw new NotSupportedException(typeof(TCommand).FullName);

                handler.Execute(command);
            }

            public Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand
            {
                var handler = (IAsyncCommandHandler<TCommand>)services.GetService(typeof(IAsyncCommandHandler<TCommand>));
                if (handler == null)
                    throw new NotSupportedException(typeof(TCommand).FullName);

                return handler.ExecuteAsync(command, cancellation);
            }
        }

        class Runner<TResult>
        {
            IServiceProvider services;

            public Runner(IServiceProvider services) => this.services = services;

            public TResult Execute<TCommand>(TCommand command) where TCommand : ICommand<TResult>
            {
                var handler = (ICommandHandler<TCommand, TResult>)services.GetService(typeof(ICommandHandler<TCommand, TResult>));
                if (handler == null)
                    throw new NotSupportedException(typeof(TCommand).FullName);

                return handler.Execute(command);
            }

            public Task<TResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand<TResult>
            {
                var handler = (IAsyncCommandHandler<TCommand, TResult>)services.GetService(typeof(IAsyncCommandHandler<TCommand, TResult>));
                if (handler == null)
                    throw new NotSupportedException(typeof(TCommand).FullName);

                return handler.ExecuteAsync(command, cancellation);
            }
        }
    }
}

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NosAyudamos
{
    [Export]
    class InteractiveAction
    {
        readonly IRepository<ActionRetryEntity> repository;
        readonly IEnvironment environment;

        public InteractiveAction(IRepository<ActionRetryEntity> repository, IEnvironment environment) 
            => (this.repository, this.environment) 
            = (repository, environment);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching general exeception because if retry pattern")]
        public async Task<TResult?> ExecuteAsync<TResult>(
            Func<Task<TResult>> action,
            Func<Task> onRetry,
            Func<Task> onCancel,
            [CallerMemberName] string actionName = "") where TResult : class
        {
            var actionId = typeof(InteractiveAction).Name;
            var actionRetry = await repository.GetAsync(actionId, actionName);

            if (actionRetry != null)
            {
                await repository.DeleteAsync(actionRetry);
            }
            else
            {
                actionRetry = new ActionRetryEntity(actionId, actionName);
            }

            try
            {
                if (actionRetry.RetryCount < environment.GetVariable<int>("ResilientNumberOfRetries", 3))
                {
                    TResult result = await action();

                    if (result == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return result;
                }
                else
                {
                    await onCancel();
                }
            }
            catch (Exception)
            {
                actionRetry.RetryCount += 1;
                await repository.PutAsync(actionRetry);
                await onRetry();
            }

            return default;
        }
    }
}

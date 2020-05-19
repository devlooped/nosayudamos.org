using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NosAyudamos
{
    [Export]
    class DurableAction
    {
        readonly IRepository<DurableActionEntity> repository;
        readonly IEnvironment environment;

        public DurableAction(IRepository<DurableActionEntity> repository, IEnvironment environment)
            => (this.repository, this.environment)
            = (repository, environment);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching general exeception because of retry pattern")]
        public async Task<TResult> ExecuteAsync<TResult>(
            Func<Task<TResult>> action,
            Func<Task> onRetry,
            Func<Task> onCancel,
            [CallerMemberName] string actionName = "") where TResult : class
        {
            var actionId = typeof(DurableAction).Name;
            var actionRetry = await repository.GetAsync(actionId, actionName);

            if (actionRetry != null)
            {
                await repository.DeleteAsync(actionRetry);
            }
            else
            {
                actionRetry = new DurableActionEntity(actionId, actionName);
            }

            try
            {
                if (actionRetry.RetryCount < environment.GetVariable("DurableActionRetries", 3))
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

#pragma warning disable CS8603 // Possible null reference return.
            return default;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}

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
            Func<int, Task<TResult>> action,
            Func<int, Task> notifyRetry,
            Func<int, Task> notifyCancel,
            [CallerMemberName] string actionName = "") where TResult : class
        {
            var actionId = typeof(DurableAction).Name;
            var actionAttempt = await repository.GetAsync(actionId, actionName);

            if (actionAttempt != null)
            {
                await repository.DeleteAsync(actionAttempt);
            }
            else
            {
                actionAttempt = new DurableActionEntity(actionId, actionName);
            }

            try
            {
                if (actionAttempt.Attempts < environment.GetVariable("DurableActionAttempts", 3))
                {
                    TResult result = await action(actionAttempt.Attempts + 1);
                    // Consider a null/default return value as a failure too.
                    if (result == default(TResult))
                        throw new InvalidOperationException();

                    return result;
                }
            }
            catch (Exception)
            {
                actionAttempt.Attempts += 1;
                await repository.PutAsync(actionAttempt);
                if (actionAttempt.Attempts == environment.GetVariable("DurableActionAttempts", 3))
                    await notifyCancel(actionAttempt.Attempts);
                else
                    await notifyRetry(actionAttempt.Attempts);
            }

#pragma warning disable CS8603 // Possible null reference return.
            return default;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}

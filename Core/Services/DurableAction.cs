using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace NosAyudamos
{
    /// <summary>
    /// Represents an action that can be retried a number of times before being 
    /// considered cancelled. The retry is durable in the sense that we persist 
    /// the number of attempts for the same operation that have already happened, 
    /// so that calling code can simply invoke the same operation multiple times, 
    /// passing what the retry/cancel callbacks should be.
    /// </summary>
    /// <remarks>
    /// A durable action is quite similar to an durable entity (or entity function, 
    /// see https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-entities)
    /// but where there is a single operation, and where there is only one discrete 
    /// piece of state: the number of retries.
    /// <para>
    /// The <c>DurableActionAttempts</c> configuration determines the number of 
    /// attemps, which defaults to 3.
    /// </para>
    /// </remarks>
    [Export]
    class DurableAction : IDurableAction
    {
        readonly IRepository<DurableActionEntity> repository;
        readonly IEnvironment env;

        public DurableAction(IEnvironment env, IRepository<DurableActionEntity> repository)
            => (this.repository, this.env)
            = (repository, env);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching general exeception because of retry pattern")]
        public async Task<TResult> ExecuteAsync<TResult>(
            string actionName,
            string actionId,
            Func<int, Task<TResult>> onExecute,
            Func<int, Task> onRetry,
            Func<int, Task> onCancel) where TResult : class
        {
            var actionAttempt = await repository.GetAsync(actionName, actionId);
            var deleteOnCompletion = actionAttempt != null;
            if (actionAttempt == null)
                actionAttempt = new DurableActionEntity(actionName, actionId);

            try
            {
                if (actionAttempt.Attempts < env.GetVariable("DurableActionAttempts", 3))
                {
                    TResult result = await onExecute(actionAttempt.Attempts + 1);
                    // Consider a null/default return value as a failure too.
                    if (result == default(TResult))
                        throw new InvalidOperationException();

                    // This means we had an existing attempt so we should clean it up.
                    if (deleteOnCompletion)
                        await repository.DeleteAsync(actionAttempt);

                    return result;
                }
            }
            catch (Exception)
            {
                actionAttempt.Attempts += 1;
                await repository.PutAsync(actionAttempt);
                if (actionAttempt.Attempts == env.GetVariable("DurableActionAttempts", 3))
                {
                    await onCancel(actionAttempt.Attempts);
                    if (deleteOnCompletion)
                        await repository.DeleteAsync(actionAttempt);
                }
                else
                {
                    await onRetry(actionAttempt.Attempts);
                }
            }

#pragma warning disable CS8603 // Possible null reference return.
            return default;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }

    interface IDurableAction
    {
        Task<TResult> ExecuteAsync<TResult>(
            string actionName,
            string actionId,
            Func<int, Task<TResult>> onExecute,
            Func<int, Task> onRetry,
            Func<int, Task> onCancel) where TResult : class;
    }
}

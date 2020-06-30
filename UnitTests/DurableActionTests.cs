using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace NosAyudamos
{
    public class DurableActionTests
    {
        [Fact]
        public async Task SimpleExecute()
        {
            var durable = new DurableAction(new Environment(), new Repository<DurableActionEntity>(CloudStorageAccount.DevelopmentStorageAccount, new Serializer()));
            var expected = new { Value = 5 };

            var result = await durable.ExecuteAsync(
                nameof(SimpleExecute), Guid.NewGuid().ToString(),
                _ => Task.FromResult(expected),
                _ => Task.CompletedTask,
                _ => Task.CompletedTask);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task ExecuteWithRetries()
        {
            var durable = new DurableAction(new Environment(), new Repository<DurableActionEntity>(CloudStorageAccount.DevelopmentStorageAccount, new Serializer()));

            var expected = new object();
            var executeCalls = new List<int>();
            var retryCalls = new List<int>();
            var cancelCalls = new List<int>();

            Func<int, Task<object>> onExecute = attempt =>
            {
                executeCalls.Add(attempt);
                if (attempt < 3)
                    throw new Exception("Failed");

                return Task.FromResult(expected);
            };

            Func<int, Task> onRetry = attempt => { retryCalls.Add(attempt); return Task.CompletedTask; };
            Func<int, Task> onCancel = attempt => { cancelCalls.Add(attempt); return Task.CompletedTask; };

            var actionId = Guid.NewGuid().ToString("n");

            var result = await durable.ExecuteAsync(nameof(ExecuteWithRetries), actionId, onExecute, onRetry, onCancel);
            // Fails once
            Assert.Null(result);

            result = await durable.ExecuteAsync(nameof(ExecuteWithRetries), actionId, onExecute, onRetry, onCancel);
            // Fails twice
            Assert.Null(result);

            result = await durable.ExecuteAsync(nameof(ExecuteWithRetries), actionId, onExecute, onRetry, onCancel);
            // Succeds on third
            Assert.Same(expected, result);

            Assert.Equal(new[] { 1, 2, 3 }, executeCalls.ToArray());
            Assert.Equal(new[] { 1, 2 }, retryCalls.ToArray());
            Assert.Empty(cancelCalls);
        }

        [Fact]
        public async Task ExecuteFailsAfterRetries()
        {
            var durable = new DurableAction(new Environment(), new Repository<DurableActionEntity>(CloudStorageAccount.DevelopmentStorageAccount, new Serializer()));

            var expected = new object();
            var executeCalls = new List<int>();
            var retryCalls = new List<int>();
            var cancelCalls = new List<int>();

            Func<int, Task<object>> onExecute = attempt => { executeCalls.Add(attempt); throw new Exception("Failed"); };
            Func<int, Task> onRetry = attempt => { retryCalls.Add(attempt); return Task.CompletedTask; };
            Func<int, Task> onCancel = attempt => { cancelCalls.Add(attempt); return Task.CompletedTask; };

            var actionId = Guid.NewGuid().ToString("n");

            var result = await durable.ExecuteAsync(nameof(ExecuteWithRetries), actionId, onExecute, onRetry, onCancel);
            Assert.Null(result);

            result = await durable.ExecuteAsync(nameof(ExecuteWithRetries), actionId, onExecute, onRetry, onCancel);
            Assert.Null(result);

            result = await durable.ExecuteAsync(nameof(ExecuteWithRetries), actionId, onExecute, onRetry, onCancel);
            Assert.Null(result);

            Assert.Equal(new[] { 1, 2, 3 }, executeCalls.ToArray());
            // Retry attemps is always executions - 1 (since the first run isn't a retry.
            Assert.Equal(new[] { 1, 2 }, retryCalls.ToArray());
            Assert.Single(cancelCalls);
        }
    }
}

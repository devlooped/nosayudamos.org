using System.Threading;
using System.Threading.Tasks;
using Merq;

namespace NosAyudamos
{
    public class ValidateTaxStatusBatch : IAsyncCommand
    {
    }

    class ValidateTaxStatusBatchHandler : IAsyncCommandHandler<ValidateTaxStatusBatch>
    {
        readonly IEntityRepository<ValidateTaxStatus> repository;
        readonly ICommandBus commands;

        public ValidateTaxStatusBatchHandler(
            ICommandBus commands,
            IEntityRepository<ValidateTaxStatus> repository)
            => (this.commands, this.repository)
            = (commands, repository);

        public bool CanExecute(ValidateTaxStatusBatch command) => true;

        public async Task ExecuteAsync(ValidateTaxStatusBatch command, CancellationToken cancellation)
        {
            await foreach (var validateCommand in repository.GetAllAsync())
            {
                await commands.ExecuteAsync(validateCommand, cancellation);
            }
        }
    }
}

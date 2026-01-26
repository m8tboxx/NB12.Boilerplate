using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Audit.Application.Commands.CleanupInboxProcessed
{
    internal sealed class CleanupInboxProcessedCommandHandler
        : IRequestHandler<CleanupInboxProcessedCommand, Result<int>>
    {
        private readonly IInboxAdminRepository _repo;

        public CleanupInboxProcessedCommandHandler(IInboxAdminRepository repo) => _repo = repo;

        public async Task<Result<int>> Handle(CleanupInboxProcessedCommand cmd, CancellationToken ct)
        {
            var max = cmd.MaxRows <= 0 ? 1000 : cmd.MaxRows;
            var deleted = await _repo.CleanupProcessedBeforeAsync(cmd.BeforeUtc, max, ct);
            return Result<int>.Success(deleted);
        }
    }
}

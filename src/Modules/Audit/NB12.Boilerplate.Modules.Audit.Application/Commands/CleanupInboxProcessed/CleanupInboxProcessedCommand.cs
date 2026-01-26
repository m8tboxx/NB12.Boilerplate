using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Audit.Application.Commands.CleanupInboxProcessed
{
    public sealed record CleanupInboxProcessedCommand(DateTime BeforeUtc, int MaxRows)
        : IRequest<Result<int>>;
}

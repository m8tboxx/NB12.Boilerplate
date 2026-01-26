using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.ReplayOutboxMessage
{
    public sealed record ReplayOutboxMessageCommand(Guid Id)
        : IRequest<Result>;
}

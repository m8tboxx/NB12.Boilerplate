using NB12.Boilerplate.BuildingBlocks.Application.Ids;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Audit.Application.Commands.ReplayInboxMessage
{
    public sealed record ReplayInboxMessageCommand(InboxMessageId Id)
        : IRequest<Result>;
}

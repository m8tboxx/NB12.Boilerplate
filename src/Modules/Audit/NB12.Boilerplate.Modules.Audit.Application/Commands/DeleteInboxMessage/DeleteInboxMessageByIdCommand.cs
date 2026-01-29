using NB12.Boilerplate.BuildingBlocks.Application.Ids;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Audit.Application.Commands.DeleteInboxMessage
{
    public sealed record DeleteInboxMessageByIdCommand(InboxMessageId Id)
        : IRequest<Result>;
}

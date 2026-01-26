using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Audit.Application.Commands.DeleteInboxMessage
{
    public sealed record DeleteInboxMessageCommand(Guid IntegrationEventId, string HandlerName)
        : IRequest<Result>;
}

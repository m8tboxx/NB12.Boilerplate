using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.DeleteOutboxMessage
{
    public sealed record DeleteOutboxMessageCommand(Guid Id)
        : IRequest<Result>;
}

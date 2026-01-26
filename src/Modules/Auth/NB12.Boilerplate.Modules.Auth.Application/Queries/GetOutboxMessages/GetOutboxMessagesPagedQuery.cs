using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Enums;
using NB12.Boilerplate.Modules.Auth.Application.Responses;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetOutboxMessages
{
    public sealed record GetOutboxMessagesPagedQuery(
        DateTime? FromUtc,
        DateTime? ToUtc,
        string? Type,
        OutboxMessageState State,
        PageRequest Page,
        Sort Sort
    ) : IRequest<Result<PagedResponse<OutboxMessageDto>>>;
}

using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Enums;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetInboxMessages
{
    public sealed record GetInboxMessagesPagedQuery(
        Guid? IntegrationEventId,
        string? HandlerName,
        InboxMessageState State,
        DateTime? FromUtc,
        DateTime? ToUtc,
        PageRequest Page,
        Sort Sort)
        : IRequest<Result<PagedResponse<InboxMessageDto>>>;
}

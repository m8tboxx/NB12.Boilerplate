using NB12.Boilerplate.BuildingBlocks.Application.Ids;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Responses;
using NB12.Boilerplate.Modules.Audit.Domain.Ids;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetInboxMessages
{
    public sealed record GetInboxMessageQuery(InboxMessageId Id)
        : IRequest<Result<InboxMessageDetailsDto>>;
}

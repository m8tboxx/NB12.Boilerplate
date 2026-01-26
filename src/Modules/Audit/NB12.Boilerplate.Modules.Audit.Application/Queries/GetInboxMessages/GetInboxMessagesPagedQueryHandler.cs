using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetInboxMessages
{
    internal sealed class GetInboxMessagesPagedQueryHandler
        : IRequestHandler<GetInboxMessagesPagedQuery, Result<PagedResponse<InboxMessageDto>>>
    {
        private readonly IInboxAdminRepository _repo;

        public GetInboxMessagesPagedQueryHandler(IInboxAdminRepository repo) => _repo = repo;

        public async Task<Result<PagedResponse<InboxMessageDto>>> Handle(GetInboxMessagesPagedQuery q, CancellationToken ct)
        {
            var page = q.Page.Normalize(defaultSize: 50, maxSize: 500);
            var result = await _repo.GetPagedAsync(
                q.IntegrationEventId, q.HandlerName, q.State, q.FromUtc, q.ToUtc, page, q.Sort, ct);

            return Result<PagedResponse<InboxMessageDto>>.Success(result);
        }
    }
}

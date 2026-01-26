using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Responses;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetOutboxMessages
{
    internal sealed class GetOutboxMessagesPagedQueryHandler
        : IRequestHandler<GetOutboxMessagesPagedQuery, Result<PagedResponse<OutboxMessageDto>>>
    {
        private readonly IOutboxAdminRepository _repo;

        public GetOutboxMessagesPagedQueryHandler(IOutboxAdminRepository repo) => _repo = repo;

        public async Task<Result<PagedResponse<OutboxMessageDto>>> Handle(GetOutboxMessagesPagedQuery q, CancellationToken ct)
        {
            var page = q.Page.Normalize(defaultSize: 50, maxSize: 500);
            var result = await _repo.GetPagedAsync(q.FromUtc, q.ToUtc, q.Type, q.State, page, q.Sort, ct);
            return Result<PagedResponse<OutboxMessageDto>>.Success(result);
        }
    }
}

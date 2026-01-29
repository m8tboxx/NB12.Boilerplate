using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetInboxMessages
{
    internal sealed class GetInboxMessageQueryHandler
        : IRequestHandler<GetInboxMessageQuery, Result<InboxMessageDetailsDto>>
    {
        private readonly IInboxAdminRepository _repo;

        public GetInboxMessageQueryHandler(IInboxAdminRepository repo) => _repo = repo;

        public async Task<Result<InboxMessageDetailsDto>> Handle(GetInboxMessageQuery q, CancellationToken ct)
        {
            var msg = await _repo.GetByIdAsync(q.Id, ct);
            if (msg is null)
            {
                return Result<InboxMessageDetailsDto>.Fail(
                    Error.NotFound("audit.inbox.not_found", $"Inbox message '{q.Id}' not found."));
            }

            return Result<InboxMessageDetailsDto>.Success(msg);
        }
    }
}

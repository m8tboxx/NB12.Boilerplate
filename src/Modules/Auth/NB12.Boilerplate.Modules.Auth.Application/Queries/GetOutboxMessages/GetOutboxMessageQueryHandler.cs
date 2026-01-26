using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Responses;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetOutboxMessages
{
    internal sealed class GetOutboxMessageQueryHandler
        : IRequestHandler<GetOutboxMessageQuery, Result<OutboxMessageDetailsDto>>
    {
        private readonly IOutboxAdminRepository _repo;

        public GetOutboxMessageQueryHandler(IOutboxAdminRepository repo) => _repo = repo;

        public async Task<Result<OutboxMessageDetailsDto>> Handle(GetOutboxMessageQuery q, CancellationToken ct)
        {
            var msg = await _repo.GetByIdAsync(q.Id, ct);
            if (msg is null)
            {
                return Result<OutboxMessageDetailsDto>.Fail(
                    Error.NotFound("auth.outbox.not_found", $"Outbox message '{q.Id}' not found."));
            }

            return Result<OutboxMessageDetailsDto>.Success(msg);
        }
    }
}

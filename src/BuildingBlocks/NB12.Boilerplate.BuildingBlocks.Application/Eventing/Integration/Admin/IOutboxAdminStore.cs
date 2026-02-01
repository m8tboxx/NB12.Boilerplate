using NB12.Boilerplate.BuildingBlocks.Application.Enums;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin
{
    public interface IOutboxAdminStore
    {
        Task<OutboxAdminStatsDto> GetStatsAsync(CancellationToken ct);

        Task<PagedResponse<OutboxAdminMessageDto>> GetPagedAsync(
            OutboxMessageState state,
            string? type,
            DateTime? fromUtc,
            DateTime? toUtc,
            PageRequest page,
            Sort sort,
            CancellationToken ct);

        Task<PagedResponse<OutboxAdminMessageDetailsDto>> GetPagedWithDetailsAsync(
            OutboxMessageState state,
            string? type,
            DateTime? fromUtc,
            DateTime? toUtc,
            PageRequest page,
            Sort sort,
            CancellationToken ct);

        Task<OutboxAdminMessageDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<OutboxAdminWriteResult> ReplayAsync(Guid id, CancellationToken ct);

        Task<OutboxAdminWriteResult> DeleteAsync(Guid id, CancellationToken ct);
    }
}

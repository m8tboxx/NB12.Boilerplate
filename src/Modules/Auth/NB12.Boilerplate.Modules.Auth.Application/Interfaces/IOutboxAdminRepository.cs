using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Modules.Auth.Application.Enums;
using NB12.Boilerplate.Modules.Auth.Application.Responses;

namespace NB12.Boilerplate.Modules.Auth.Application.Interfaces
{
    public interface IOutboxAdminRepository
    {
        Task<PagedResponse<OutboxMessageDto>> GetPagedAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? type,
            OutboxMessageState state,
            PageRequest page,
            Sort sort,
            CancellationToken ct);

        Task<PagedResponse<OutboxMessageDetailsDto>> GetPagedWithDetailsAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? type,
            OutboxMessageState state,
            PageRequest page,
            Sort sort,
            CancellationToken ct);

        Task<OutboxMessageDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<OutboxStatsDto> GetStatsAsync(CancellationToken ct);

        Task<bool> ReplayAsync(Guid id, CancellationToken ct);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}

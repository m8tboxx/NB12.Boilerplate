using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Modules.Audit.Application.Enums;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Interfaces
{
    public interface IInboxAdminRepository
    {
        Task<PagedResponse<InboxMessageDto>> GetPagedAsync(
            Guid? integrationEventId,
            string? handlerName,
            InboxMessageState state,
            DateTime? fromUtc,
            DateTime? toUtc,
            PageRequest page,
            Sort sort,
            CancellationToken ct);

        Task<InboxStatsDto> GetStatsAsync(CancellationToken ct);

        Task<bool> DeleteAsync(Guid integrationEventId, string handlerName, CancellationToken ct);

        Task<int> CleanupProcessedBeforeAsync(DateTime beforeUtc, int maxRows, CancellationToken ct);
    }
}

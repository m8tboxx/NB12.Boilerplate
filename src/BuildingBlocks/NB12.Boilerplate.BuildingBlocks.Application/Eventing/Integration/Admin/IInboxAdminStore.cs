using NB12.Boilerplate.BuildingBlocks.Application.Enums;
using NB12.Boilerplate.BuildingBlocks.Application.Ids;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin
{
    public interface IInboxAdminStore
    {
        Task<InboxAdminStatsDto> GetStatsAsync(CancellationToken ct);

        Task<PagedResponse<InboxAdminMessageDto>> GetPagedAsync(
            Guid? integrationEventId,
            string? handlerName,
            InboxMessageState state,
            DateTime? fromUtc,
            DateTime? toUtc,
            PageRequest page,
            Sort sort,
            CancellationToken ct);

        Task<InboxAdminMessageDetailsDto?> GetByIdAsync(InboxMessageId id, CancellationToken ct);

        Task<InboxAdminWriteResult> ReplayAsync(InboxMessageId id, CancellationToken ct);

        Task<InboxAdminWriteResult> DeleteAsync(InboxMessageId id, CancellationToken ct);

        Task<InboxAdminWriteResult> DeleteAsync(Guid integrationEventId, string handlerName, CancellationToken ct);

        Task<int> CleanupProcessedBeforeAsync(DateTime beforeUtc, int maxRows, CancellationToken ct);
    }
}

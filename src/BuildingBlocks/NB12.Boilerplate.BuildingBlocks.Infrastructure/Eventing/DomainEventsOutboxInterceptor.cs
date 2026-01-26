using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Domain;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Domain.Events;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Ids;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox;
using System.Text.Json;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Eventing
{
    /// <summary>
    /// Writes integration events to the Outbox table during SaveChanges and dispatches domain events
    /// in-process after a successful commit.
    /// </summary>
    public sealed class DomainEventsOutboxInterceptor(
        IDomainEventDispatcher dispatcher,
        CompositeDomainEventToIntegrationEventMapper mapper,
        JsonSerializerOptions jsonOptions) : SaveChangesInterceptor
    {
        private readonly List<IDomainEvent> _capturedDomainEvents = [];
        private readonly List<IHasDomainEvents> _capturedEntities = [];

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context is { } db)
                CaptureAndWriteOutbox(db);

            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is { } db)
                CaptureAndWriteOutbox(db);

            return ValueTask.FromResult(result);
        }

        private void CaptureAndWriteOutbox(DbContext db)
        {
            // Reset state for this SaveChanges call (defensive).
            _capturedDomainEvents.Clear();
            _capturedEntities.Clear();

            var entities = db.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count > 0)
                .ToList();

            if (entities.Count == 0)
                return;

            _capturedEntities.AddRange(entities);

            var domainEvents = entities
                .SelectMany(e => e.DomainEvents)
                .ToList();

            _capturedDomainEvents.AddRange(domainEvents);

            var outboxMessages = new List<OutboxMessage>();

            foreach (var de in domainEvents)
            {
                var integrationEvents = mapper.MapAll(de);

                foreach (var ie in integrationEvents)
                {
                    outboxMessages.Add(new OutboxMessage(
                        new OutboxMessageId(ie.Id),
                        ie.OccurredAtUtc,
                        ie.GetType().FullName ?? ie.GetType().Name,
                        JsonSerializer.Serialize(ie, ie.GetType(), jsonOptions),
                        attemptCount: 0,
                        processedAtUtc: null,
                        lastError: null,
                        lockedUntilUtc: null,
                        lockedBy: null,
                        deadLetteredAtUtc: null,
                        deadLetterReason: null));
                }
            }

            if (outboxMessages.Count > 0)
                db.Set<OutboxMessage>().AddRange(outboxMessages);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            AfterCommitDispatch(CancellationToken.None);
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            await AfterCommitDispatchAsync(cancellationToken);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            _capturedDomainEvents.Clear();
            _capturedEntities.Clear();
            base.SaveChangesFailed(eventData);
        }

        private void AfterCommitDispatch(CancellationToken ct)
        {
            if (_capturedDomainEvents.Count == 0)
                return;

            var toDispatch = _capturedDomainEvents.ToList();
            _capturedDomainEvents.Clear();

            foreach (var entity in _capturedEntities)
                entity.ClearDomainEvents();

            _capturedEntities.Clear();

            dispatcher.Dispatch(toDispatch, ct).GetAwaiter().GetResult();
        }

        private async Task AfterCommitDispatchAsync(CancellationToken ct)
        {
            if (_capturedDomainEvents.Count == 0)
                return;

            var toDispatch = _capturedDomainEvents.ToList();
            _capturedDomainEvents.Clear();

            foreach (var entity in _capturedEntities)
                entity.ClearDomainEvents();

            _capturedEntities.Clear();

            await dispatcher.Dispatch(toDispatch, ct);
        }
    }
}

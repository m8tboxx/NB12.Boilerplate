using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
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
        JsonSerializerOptions jsonOptions,
        ILogger<DomainEventsOutboxInterceptor> logger) : SaveChangesInterceptor
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

 
        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            var batch = DrainCapturedEvents();

            if (batch is not null)
                _ = DispatchSafeAsync(batch, CancellationToken.None);

            return base.SavedChanges(eventData, result);
        }


        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            var batch = DrainCapturedEvents();

            if (batch is not null)
                _ = DispatchSafeAsync(batch, cancellationToken);

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }


        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            _capturedDomainEvents.Clear();
            _capturedEntities.Clear();
            base.SaveChangesFailed(eventData);
        }


        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            _capturedDomainEvents.Clear();
            _capturedEntities.Clear();
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }


        private List<IDomainEvent>? DrainCapturedEvents()
        {
            if (_capturedDomainEvents.Count == 0)
                return null;

            var toDispatch = _capturedDomainEvents.ToList();
            _capturedDomainEvents.Clear();

            foreach (var entity in _capturedEntities)
                entity.ClearDomainEvents();

            _capturedEntities.Clear();
            return toDispatch;
        }


        private async Task DispatchSafeAsync(List<IDomainEvent> events, CancellationToken ct)
        {
            try
            {
                await dispatcher.Dispatch(events, ct);
            }
            catch (Exception ex)
            {
                // IMPORTANT: After committing, this must not break any requests.
                logger.LogError(ex,
                    "Post-commit domain event dispatch failed. Events={EventCount}",
                    events.Count);
            }
        }


        private void CaptureAndWriteOutbox(DbContext db)
        {
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

            foreach (var domainEvent in domainEvents)
            {
                var integrationEvents = mapper.MapAll(domainEvent);

                foreach (var integrationEvent in integrationEvents)
                {
                    outboxMessages.Add(new OutboxMessage(
                        new OutboxMessageId(integrationEvent.Id),
                        integrationEvent.OccurredAtUtc,
                        integrationEvent.GetType().FullName ?? integrationEvent.GetType().Name,
                        JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), jsonOptions),
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
    }
}

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
    public sealed class DomainEventsOutboxInterceptor(
        IDomainEventDispatcher dispatcher,
        CompositeDomainEventToIntegrationEventMapper mapper,
        JsonSerializerOptions jsonOptions) : SaveChangesInterceptor
    {
        private readonly List<IDomainEvent> _capturedDomainEvents = [];

        // ---- Sync hook (SaveChanges) ----
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context is { } db)
                CaptureAndWriteOutbox(db);

            return result;
        }

        // ---- Async hook (SaveChangesAsync) ----
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is { } db)
                CaptureAndWriteOutbox(db);

            return ValueTask.FromResult(result);
        }

        // Shared logic for both sync/async saving hooks
        private void CaptureAndWriteOutbox(DbContext db)
        {
            var hasDomainEventsEntries = db.ChangeTracker.Entries()
        .Where(e => e.Entity is IHasDomainEvents)
        .Select(e => (Entity: (IHasDomainEvents)e.Entity, Type: e.Entity.GetType().FullName))
        .ToList();

            Console.WriteLine($">>> IHasDomainEvents entries: {hasDomainEventsEntries.Count}");
            foreach (var e in hasDomainEventsEntries)
                Console.WriteLine($">>>  - {e.Type} DomainEvents={e.Entity.DomainEvents.Count}");

            // JETZT erst filtern auf wirklich vorhandene DomainEvents
            var entitiesWithEvents = hasDomainEventsEntries
                .Where(x => x.Entity.DomainEvents.Count > 0)
                .ToList();

            Console.WriteLine($">>> Entities WITH DomainEvents: {entitiesWithEvents.Count}");

            var entities = db.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .ToList();

            if (entities.Count == 0)
                return;

            var domainEvents2 = entitiesWithEvents.SelectMany(x => x.Entity.DomainEvents).ToList();
            Console.WriteLine($">>> DomainEvents total: {domainEvents2.Count}");

            var domainEvents = entities.SelectMany(e => e.Entity.DomainEvents).ToList();
            _capturedDomainEvents.AddRange(domainEvents);

            var outboxMessages = new List<OutboxMessage>();
            var mappedCount = 0;

            foreach (var de in domainEvents)
            {
                var ies = mapper.MapAll(de);
                mappedCount += ies.Count;

                foreach (var ie in ies)
                {
                    outboxMessages.Add(new OutboxMessage(
                        new OutboxMessageId(ie.Id),
                        ie.OccurredAtUtc,
                        ie.GetType().FullName ?? ie.GetType().Name,
                        JsonSerializer.Serialize(ie, ie.GetType(), jsonOptions),
                        attemptCount: 0,
                        processedAtUtc: null,
                        lastError: null));
                }
            }

            Console.WriteLine($">>> IntegrationEvents mapped: {mappedCount}");
            Console.WriteLine($">>> OutboxMessages to add: {outboxMessages.Count}");

            if (outboxMessages.Count > 0)
                db.Set<OutboxMessage>().AddRange(outboxMessages);

            // Clear domain events so they don't get re-processed
            foreach (var entry in entities)
                entry.Entity.ClearDomainEvents();

            // optional: check tracker
            var added = db.ChangeTracker.Entries<OutboxMessage>()
                .Count(x => x.State == EntityState.Added);

            Console.WriteLine($">>> OutboxMessage Added in tracker: {added}");
        }

        // After commit: in-process dispatch
        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (_capturedDomainEvents.Count > 0)
            {
                var toDispatch = _capturedDomainEvents.ToList();
                _capturedDomainEvents.Clear();

                await dispatcher.Dispatch(toDispatch, cancellationToken);
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            _capturedDomainEvents.Clear();
            base.SaveChangesFailed(eventData);
        }
    }
}

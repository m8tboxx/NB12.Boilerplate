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

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var db = eventData.Context;
            if (db is null) return result;

            var entities = db.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .ToList();

            if (entities.Count == 0) return result;

            var domainEvents = entities.SelectMany(e => e.Entity.DomainEvents).ToList();
            _capturedDomainEvents.AddRange(domainEvents);

            // Create Outbox messages inside same transaction
            var outboxMessages = new List<OutboxMessage>();

            foreach (var de in domainEvents)
            {
                var ies = mapper.MapAll(de);
                foreach (var ie in ies)
                {
                    outboxMessages.Add(new OutboxMessage(
                        new OutboxMessageId(ie.Id),
                        ie.OccurredAtUtc,
                        ie.GetType().FullName ?? ie.GetType().Name,
                        JsonSerializer.Serialize(ie, ie.GetType(), jsonOptions),
                        0,
                        null,
                        null));
                }
            }

            if (outboxMessages.Count > 0)
                db.Set<OutboxMessage>().AddRange(outboxMessages);

            // Clear domain events (prevent double processing)
            foreach (var entry in entities)
                entry.Entity.ClearDomainEvents();

            return result;
        }

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

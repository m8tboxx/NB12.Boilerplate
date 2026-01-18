using NB12.Boilerplate.BuildingBlocks.Domain.Interfaces;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Common
{
    /// <summary>
    /// Provides a base implementation for domain events, supplying a unique identifier and occurrence timestamp.
    /// </summary>
    /// <remarks>
    /// Each instance is initialized with a new <see cref="Guid"/> for <see cref="Id"/> and a UTC timestamp
    /// for <see cref="OccurredAt"/> using <see cref="DateTime.UtcNow"/>. This supports consistent event tracing
    /// and ordering across the domain.
    /// </remarks>
    public class DomainEventBase : IDomainEvent
    {
        public Guid Id { get; }

        public DateTime OccurredAt { get; }

        public DomainEventBase()
        {
            this.Id = Guid.NewGuid();
            this.OccurredAt = DateTime.UtcNow;
        }
    }
}

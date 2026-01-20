using NB12.Boilerplate.BuildingBlocks.Domain.Events;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Common
{
    public abstract class BaseEntity<TId>
        where TId : notnull
    {
        public TId Id { get; protected set; } = default!;

        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected BaseEntity() { } // EF
        protected BaseEntity(TId id)
        {
            Id = id;
        }

        protected void AddDomainEvent(IDomainEvent @event)
            => _domainEvents.Add(@event);
        public void ClearDomainEvents() => _domainEvents.Clear();

        public override bool Equals(object? obj)
        {
            if (obj is not BaseEntity<TId> other)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            // Prevent transient entities (default id) from being equal
            if (IsTransient() || other.IsTransient())
                return false;

            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
            => IsTransient() ? base.GetHashCode() : Id.GetHashCode();

        public static bool operator ==(BaseEntity<TId>? left, BaseEntity<TId>? right)
            => Equals(left, right);

        public static bool operator !=(BaseEntity<TId>? left, BaseEntity<TId>? right)
            => !Equals(left, right);

        private bool IsTransient()
            => Equals(Id, default(TId)!);
    }
}

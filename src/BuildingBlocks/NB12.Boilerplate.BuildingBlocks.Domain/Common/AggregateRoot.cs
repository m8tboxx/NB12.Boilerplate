using NB12.Boilerplate.BuildingBlocks.Domain.Interfaces;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Common
{
    public abstract class AggregateRoot<TId> : BaseAuditableEntity<TId>, IAggregateRoot
        where TId : notnull
    {
        protected AggregateRoot() { } // EF

        protected AggregateRoot(TId id, DateTime createdUtc, string? createdBy)
            : base(id, createdUtc, createdBy)
        {
        }
    }
}

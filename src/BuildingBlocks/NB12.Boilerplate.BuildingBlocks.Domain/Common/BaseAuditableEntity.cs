using NB12.Boilerplate.BuildingBlocks.Domain.Interfaces;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Common
{
    public abstract class BaseAuditableEntity<TId> : BaseEntity<TId>
        where TId : notnull
    {
        public DateTime CreatedAtUtc { get; private set; }
        public string? CreatedBy { get; private set; }

        public DateTime? LastModifiedAtUtc { get; private set; }
        public string? LastModifiedBy { get; private set; }

        protected BaseAuditableEntity() { } // EF

        protected BaseAuditableEntity(TId id, DateTime createdUtc, string? createdBy)
        : base(id)
        {
            CreatedAtUtc = createdUtc;
            CreatedBy = createdBy;
        }

        public void SetCreated(DateTime utcNow, string? actor)
        {
            if (CreatedAtUtc != default)
                return; // oder throw, wenn du harte Regeln willst

            CreatedAtUtc = utcNow;
            CreatedBy = actor;
        }

        public void SetModified(DateTime utcNow, string? actor)
        {
            LastModifiedAtUtc = utcNow;
            LastModifiedBy = actor;
        }
    }
}

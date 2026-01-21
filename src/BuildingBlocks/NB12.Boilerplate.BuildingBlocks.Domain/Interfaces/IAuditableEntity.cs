namespace NB12.Boilerplate.BuildingBlocks.Domain.Interfaces
{
    public interface IAuditableEntity
    {
        DateTime CreatedAtUtc { get; }
        string? CreatedBy { get; }

        DateTime? LastModifiedAtUtc { get; }
        string? LastModifiedBy { get; }

        void SetCreated(DateTime utcNow, string? actor);
        void SetModified(DateTime utcNow, string? actor);

        string GetAuditEntityId();
    }
}

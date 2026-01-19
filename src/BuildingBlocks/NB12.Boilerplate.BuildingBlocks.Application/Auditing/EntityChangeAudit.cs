namespace NB12.Boilerplate.BuildingBlocks.Application.Auditing
{
    public sealed record EntityChangeAudit(
        string EntityType,
        string EntityId,
        AuditOperation Operation,
        IReadOnlyCollection<PropertyChange> Changes);
}

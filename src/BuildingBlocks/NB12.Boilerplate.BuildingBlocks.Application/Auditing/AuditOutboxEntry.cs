namespace NB12.Boilerplate.BuildingBlocks.Application.Auditing
{
    public sealed record AuditOutboxEntry(
        string EntityType,
        string EntityId,
        string Operation,
        IReadOnlyCollection<AuditOutboxPropertyChange> Changes);

    public sealed record AuditOutboxPropertyChange(
    string Name,
    object? Old,
    object? New);
}

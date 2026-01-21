namespace NB12.Boilerplate.Modules.Audit.Contracts.Auditing
{
    public sealed record AuditableEntityChange(
    string EntityType,
    string EntityId,
    string Operation,
    IReadOnlyCollection<AuditPropertyChange> Changes);

    public sealed record AuditPropertyChange(
    string Name,
    object? Old,
    object? New
);
}

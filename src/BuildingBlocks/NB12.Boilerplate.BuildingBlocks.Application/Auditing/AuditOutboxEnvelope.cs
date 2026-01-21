namespace NB12.Boilerplate.BuildingBlocks.Application.Auditing
{
    public sealed record AuditOutboxEnvelope(
    string Module,
    AuditContext Context,
    IReadOnlyCollection<AuditOutboxEntry> Entries);
}

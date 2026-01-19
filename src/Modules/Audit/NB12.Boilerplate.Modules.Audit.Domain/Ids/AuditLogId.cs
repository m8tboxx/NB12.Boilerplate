using NB12.Boilerplate.BuildingBlocks.Domain.Abstractions;

namespace NB12.Boilerplate.Modules.Audit.Domain.Ids
{
    public sealed record AuditLogId(Guid Value) : StronglyTypedId<Guid>(Value)
    {
        public static AuditLogId New()
        {
            var id = Guid.NewGuid();
            ThrowIfDefault(id, nameof(id));
            return new AuditLogId(id);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}

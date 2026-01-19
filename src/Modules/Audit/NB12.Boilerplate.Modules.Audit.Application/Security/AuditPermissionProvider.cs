using NB12.Boilerplate.BuildingBlocks.Application.Security;

namespace NB12.Boilerplate.Modules.Audit.Application.Security
{
    public sealed class AuditPermissionProvider : IPermissionProvider
    {
        public IReadOnlyList<PermissionDefinition> GetAll() => AuditPermissions.All;
    }
}

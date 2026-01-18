using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Security
{
    public sealed class PermissionCatalog : IPermissionCatalog
    {
        public IReadOnlyList<PermissionDefinition> GetAll()
            => Permissions.All;
    }
}

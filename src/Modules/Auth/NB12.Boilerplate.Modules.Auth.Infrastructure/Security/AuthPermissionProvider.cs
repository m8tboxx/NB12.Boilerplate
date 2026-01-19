using NB12.Boilerplate.BuildingBlocks.Application.Security;
using NB12.Boilerplate.Modules.Auth.Application.Security;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Security
{
    public sealed class AuthPermissionProvider : IPermissionProvider
    {
        public IReadOnlyList<PermissionDefinition> GetAll() => AuthPermissions.All;
    }
}

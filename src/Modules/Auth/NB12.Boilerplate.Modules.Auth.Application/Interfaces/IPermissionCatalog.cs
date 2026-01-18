using NB12.Boilerplate.Modules.Auth.Application.Security;

namespace NB12.Boilerplate.Modules.Auth.Application.Interfaces
{
    public interface IPermissionCatalog
    {
        IReadOnlyList<PermissionDefinition> GetAll();
    }
}

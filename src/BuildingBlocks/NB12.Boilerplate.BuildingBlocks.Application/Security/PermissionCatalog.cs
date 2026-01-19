using NB12.Boilerplate.BuildingBlocks.Application.Security;

namespace NB12.Boilerplate.Modules.Auth.Application.Security
{
    public sealed class PermissionCatalog : IPermissionCatalog
    {
        private readonly IEnumerable<IPermissionProvider> _providers;

        public PermissionCatalog(IEnumerable<IPermissionProvider> providers)
        {
            _providers = providers;
        }
        public IReadOnlyList<PermissionDefinition> GetAll()
        {
            return _providers
                .SelectMany(p => p.GetAll ())
                .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Key)
                .ToList();
        }
           
    }
}

namespace NB12.Boilerplate.BuildingBlocks.Application.Security
{
    public interface IPermissionCatalog
    {
        IReadOnlyList<PermissionDefinition> GetAll();
    }
}

namespace NB12.Boilerplate.BuildingBlocks.Application.Security
{
    public interface IPermissionProvider
    {
        IReadOnlyList<PermissionDefinition> GetAll();
    }
}

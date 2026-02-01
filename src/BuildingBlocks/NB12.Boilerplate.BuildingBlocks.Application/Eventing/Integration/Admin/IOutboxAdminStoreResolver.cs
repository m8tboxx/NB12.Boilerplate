namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin
{
    public interface IOutboxAdminStoreResolver
    {
        IOutboxAdminStore GetRequired(string moduleKey);
        bool TryGet(string moduleKey, out IOutboxAdminStore store);
    }
}

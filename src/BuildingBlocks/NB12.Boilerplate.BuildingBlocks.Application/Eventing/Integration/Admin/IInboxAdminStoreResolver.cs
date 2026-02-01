namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin
{
    public interface IInboxAdminStoreResolver
    {
        IInboxAdminStore GetRequired(string moduleKey);
        bool TryGet(string moduleKey, out IInboxAdminStore store);
    }
}

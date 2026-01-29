namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    /// <summary>
    /// Resolves the correct inbox store for a given module key.
    /// </summary>
    public interface IInboxStoreResolver
    {
        IInboxStore Get(string moduleKey);
    }
}

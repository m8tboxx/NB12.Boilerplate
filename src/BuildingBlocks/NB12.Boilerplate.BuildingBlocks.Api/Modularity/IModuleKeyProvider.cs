namespace NB12.Boilerplate.BuildingBlocks.Api.Modularity
{
    /// <summary>
    /// Stable identifier for a module that is used for keyed services and named options.
    /// Must match configuration keys, e.g. InboxCleanup:Modules:{ModuleKey}.
    /// </summary>
    public interface IModuleKeyProvider
    {
        string ModuleKey { get; }
    }
}

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    /// <summary>
    /// Optional explicit module key for integration handlers.
    /// If missing, module key is inferred from namespace "...Modules.<Module>..."
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class IntegrationHandlerModuleAttribute : Attribute
    {
        public string ModuleKey { get; }

        public IntegrationHandlerModuleAttribute(string moduleKey)
        {
            if (string.IsNullOrWhiteSpace(moduleKey))
                throw new ArgumentException("Module key must be provided.", nameof(moduleKey));

            ModuleKey = moduleKey;
        }
    }
}

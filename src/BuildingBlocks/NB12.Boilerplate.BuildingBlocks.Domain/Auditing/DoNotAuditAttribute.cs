namespace NB12.Boilerplate.BuildingBlocks.Domain.Auditing
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class DoNotAuditAttribute : Attribute { }
}

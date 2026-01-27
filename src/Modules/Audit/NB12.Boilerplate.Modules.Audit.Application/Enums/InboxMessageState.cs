namespace NB12.Boilerplate.Modules.Audit.Application.Enums
{
    public enum InboxMessageState
    {
        All = 0,
        Pending = 1,
        Failed = 2,
        Processed = 3,
        DeadLettered = 4
    }
}

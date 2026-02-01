namespace NB12.Boilerplate.BuildingBlocks.Application.Enums
{
    public enum OutboxMessageState
    {
        All = 0,
        Pending = 1,
        Failed = 2,
        Processed = 3,
        DeadLettered = 4
    }
}

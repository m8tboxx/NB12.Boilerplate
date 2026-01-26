namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public enum OutboxFailureAction
    {
        Retry = 0,
        DeadLetter = 1
    }
}

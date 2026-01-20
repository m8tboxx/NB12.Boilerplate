namespace NB12.Boilerplate.BuildingBlocks.Domain.Events
{
    public interface IDomainEvent
    {
        Guid Id { get; }
        DateTime OccurredAtUtc { get; }
    }
}

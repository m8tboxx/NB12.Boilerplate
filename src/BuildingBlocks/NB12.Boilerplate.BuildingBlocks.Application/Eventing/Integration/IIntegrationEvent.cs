namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public interface IIntegrationEvent
    {
        Guid Id { get; }
        DateTime OccurredAtUtc { get; }
    }
}

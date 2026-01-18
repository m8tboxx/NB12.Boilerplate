using MediatR;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Interfaces
{
    public interface IDomainEvent : INotification
    {
        Guid Id { get; }
        DateTime OccurredAt { get; }
    }
}

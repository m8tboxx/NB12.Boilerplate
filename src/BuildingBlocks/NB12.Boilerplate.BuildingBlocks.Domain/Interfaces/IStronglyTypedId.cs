namespace NB12.Boilerplate.BuildingBlocks.Domain.Interfaces
{
    public interface IStronglyTypedId<out TValue>
    {
        TValue Value { get; }
    }
}

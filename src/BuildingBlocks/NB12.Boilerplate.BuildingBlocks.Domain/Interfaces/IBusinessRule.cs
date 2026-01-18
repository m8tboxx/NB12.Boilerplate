namespace NB12.Boilerplate.BuildingBlocks.Domain.Interfaces
{
    public interface IBusinessRule
    {
        bool IsBroken();
        string Message { get; }
    }
}

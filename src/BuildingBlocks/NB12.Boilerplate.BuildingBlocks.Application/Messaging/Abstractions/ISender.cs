namespace NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions
{
    public interface ISender
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
    }
}

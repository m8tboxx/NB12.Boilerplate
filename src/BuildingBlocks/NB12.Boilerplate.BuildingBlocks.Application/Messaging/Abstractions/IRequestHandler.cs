namespace NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions
{
    public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken ct);
    }
}

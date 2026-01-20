namespace NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions
{
    public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    {
        Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
    }
}

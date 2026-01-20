namespace NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions
{
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
}

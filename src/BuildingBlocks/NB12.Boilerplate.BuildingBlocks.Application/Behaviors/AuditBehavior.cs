using Microsoft.Extensions.Logging;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using System.Diagnostics;

namespace NB12.Boilerplate.BuildingBlocks.Application.Behaviors
{
    public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;
        private readonly ICurrentUser _currentUser;

        public AuditBehavior(
            ILogger<AuditBehavior<TRequest, TResponse>> logger,
            ICurrentUser currentUser)
        {
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var response = await next();
                sw.Stop();

                _logger.LogInformation(
                    "Request {RequestName} handled in {ElapsedMs}ms by {UserId}",
                    typeof(TRequest).Name,
                    sw.ElapsedMilliseconds,
                    _currentUser.UserId ?? "anonymous");

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(
                    ex,
                    "Request {RequestName} failed in {ElapsedMs}ms by {UserId}",
                    typeof(TRequest).Name,
                    sw.ElapsedMilliseconds,
                    _currentUser.UserId ?? "anonymous");
                throw;
            }
        }
    }
}

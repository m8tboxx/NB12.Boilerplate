using FluentValidation;
using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.BuildingBlocks.Application.Behaviors
{
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
            => _validators = validators;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)));

            var failures = results
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count == 0)
                return await next();

            var errors = failures.Select(f =>
                Error.Validation(
                    code: $"validation.{f.PropertyName}",
                    message: f.ErrorMessage,
                    meta: new Dictionary<string, object?> { ["property"] = f.PropertyName, ["attemptedValue"] = f.AttemptedValue }
                )).ToList();

            // Return Result/Result<T> failures without throwing
            if (typeof(TResponse) == typeof(Result))
                return (TResponse)(object)Result.Fail(errors);

            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var t = typeof(TResponse).GetGenericArguments()[0];
                var resultType = typeof(Result<>).MakeGenericType(t);

                var failMethod = resultType.GetMethod(
                    name: "Fail",
                    types: new[] { typeof(IEnumerable<Error>) });

                if (failMethod is null)
                    throw new InvalidOperationException("Result<T>.Fail(IEnumerable<Error>) not found.");

                return (TResponse)failMethod.Invoke(null, new object[] { errors })!;
            }

            // Wenn du Non-Result Responses zulässt: dann hier throw, damit ExceptionHandler greift
            throw new ValidationException(failures);
        }
    }
}

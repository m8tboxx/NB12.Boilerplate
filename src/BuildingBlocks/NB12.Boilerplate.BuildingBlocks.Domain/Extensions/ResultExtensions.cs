using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Extensions
{
    public static class ResultExtensions
    {
        // Bind extensions methods for Result
        public static Result Bind(this Result result, Func<Result> next)
            => result.IsFailure ? result : next();

        public static Result<TOut> Bind<TOut>(this Result result, Func<Result<TOut>> next)
            => result.IsFailure ? Result<TOut>.Fail(result.Errors) : next();

        public static Result Bind<TIn>(this Result<TIn> result, Func<TIn, Result> next)
            => result.IsFailure ? Result.Fail(result.Errors) : next(result.Value);

        public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> next)
        => result.IsFailure ? Result<TOut>.Fail(result.Errors) : next(result.Value);

        // ----- Map -----
        public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map)
            => result.IsFailure ? Result<TOut>.Fail(result.Errors) : Result<TOut>.Success(map(result.Value));

        // ----- Tap (Side effects) -----
        public static Result Tap(this Result result, Action action)
        {
            if (result.IsSuccess) action();
            return result;
        }

        public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
        {
            if (result.IsSuccess) action(result.Value);
            return result;
        }

        // ----- Ensure (Guard in the middle of the railway) -----
        public static Result Ensure(this Result result, Func<bool> predicate, Error error)
            => result.IsFailure ? result : (predicate() ? result : Result.Fail(error));

        public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
            => result.IsFailure ? result : (predicate(result.Value) ? result : Result<T>.Fail(error));

        // ----- Recover (Fallback) -----
        public static Result Recover(this Result result, Func<IReadOnlyList<Error>, Result> fallback)
            => result.IsSuccess ? result : fallback(result.Errors);

        public static Result<T> Recover<T>(this Result<T> result, Func<IReadOnlyList<Error>, Result<T>> fallback)
            => result.IsSuccess ? result : fallback(result.Errors);

        // ----- Async Bind/Map -----
        public static async Task<Result> BindAsync(this Result result, Func<Task<Result>> next)
            => result.IsFailure ? result : await next().ConfigureAwait(false);

        public static async Task<Result<TOut>> BindAsync<TOut>(this Result result, Func<Task<Result<TOut>>> next)
            => result.IsFailure ? Result<TOut>.Fail(result.Errors) : await next().ConfigureAwait(false);

        public static async Task<Result> BindAsync<TIn>(this Result<TIn> result, Func<TIn, Task<Result>> next)
            => result.IsFailure ? Result.Fail(result.Errors) : await next(result.Value).ConfigureAwait(false);

        public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> next)
            => result.IsFailure ? Result<TOut>.Fail(result.Errors) : await next(result.Value).ConfigureAwait(false);

        public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<TOut>> map)
            => result.IsFailure ? Result<TOut>.Fail(result.Errors) : Result<TOut>.Success(await map(result.Value).ConfigureAwait(false));
    }
}

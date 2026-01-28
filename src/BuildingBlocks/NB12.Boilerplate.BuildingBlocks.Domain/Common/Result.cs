namespace NB12.Boilerplate.BuildingBlocks.Domain.Common
{
    public class Result
    {
        private readonly List<Error> _errors;
        public IReadOnlyList<Error> Errors => _errors;
        public Error? PrimaryError => _errors.Count > 0 ? _errors[0] : null;

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        protected Result(bool isSuccess, List<Error>? errors = null)
        {
            IsSuccess = isSuccess;
            _errors = errors ?? [];

            if (IsSuccess && _errors.Count > 0)
                throw new InvalidOperationException("Successful result must not contain errors.");

            if (!IsSuccess && _errors.Count == 0)
                throw new InvalidOperationException("Failed result must contain at least one error.");
        }

        public static Result Success() 
            => new(true);

        public static Result Fail(Error error) 
            => new(false, new List<Error> { error });

        public static Result Fail(IEnumerable<Error> errors)
            => new(false, [.. errors]);

        public static Result Combine(params Result[] results)
        {
            var errors = results.Where(r => r.IsFailure).SelectMany(r => r.Errors).ToList();
            return errors.Count == 0 ? Success() : Fail(errors);
        }

        /// <summary>
        /// Create-Pattern for "startpoint" of Railway: Condition must be success.
        /// </summary>
        public static Result Create(bool condition, Error errorIfFalse)
            => condition ? Success() : Fail(errorIfFalse);

    }

    public sealed class Result<T> : Result
    {
        private readonly T? _value;
        
        private Result (bool isSuccess, T? value, List<Error>? errors): base(isSuccess, errors)
        {
            _value = value;
        }

        public T Value => IsSuccess 
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

        public static Result<T> Success(T value) => new(true, value, null);

        public static new Result<T> Fail(Error error) => new(false, default, new List<Error> { error });

        public static new Result<T> Fail(IEnumerable<Error> errors) => new(false, default, [.. errors]);

        /// <summary>
        /// Create pattern: generates a result from Value + Validation.
        /// </summary>
        public static Result<T> Create(T? value, Func<T, Result>? validate = null, Error? nullError = null)
        {
            if (value is null)
                return Fail(nullError ?? Error.Validation("value.null", "Value must not be null."));

            if (validate is null)
                return Success(value);

            var validation = validate(value);
            return validation.IsSuccess ? Success(value) : Fail(validation.Errors);
        }
    }
}

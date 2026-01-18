using NB12.Boilerplate.BuildingBlocks.Domain.Interfaces;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Abstractions
{
    public abstract record StronglyTypedId<TValue>(TValue Value) : IStronglyTypedId<TValue>
        where TValue : notnull
    {
        public override string ToString() => Value.ToString() ?? string.Empty;

        protected static void ThrowIfDefault(TValue value, string? paramName = null)
        {
            if (EqualityComparer<TValue>.Default.Equals(value, default!))
                throw new ArgumentException("ID value must not be default.", paramName ?? nameof(value));
        }
    }
}

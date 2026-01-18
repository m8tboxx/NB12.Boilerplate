using System.ComponentModel;
using System.Globalization;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Ids
{
    /// <summary>
    /// Provides a <see cref="TypeConverter"/> for strongly-typed identifier types by converting between their
    /// string representation and the underlying scalar value.
    /// </summary>
    /// <remarks>
    /// This converter supports conversion <b>from</b> <see cref="string"/> to <typeparamref name="TId"/> by parsing
    /// the string into <typeparamref name="TValue"/> (special-casing <see cref="Guid"/> via <see cref="Guid.Parse"/>),
    /// then constructing the identifier using <see cref="Activator.CreateInstance(Type, object?[])"/>.
    /// It also supports conversion <b>to</b> <see cref="string"/> by reflecting a public <c>Value</c> property on the
    /// identifier instance and returning its string representation. For non-string conversions, default
    /// <see cref="TypeConverter"/> behavior is used.
    /// </remarks>
    public sealed class StronglyTypedIdTypeConverter<TId, TValue> : TypeConverter
        where TValue : notnull
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string s)
            {
                object parsed = typeof(TValue) == typeof(Guid)
                    ? Guid.Parse(s)
                    : Convert.ChangeType(s, typeof(TValue), culture ?? CultureInfo.InvariantCulture)!;

                return Activator.CreateInstance(typeof(TId), parsed)!;
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is not null)
            {
                var valueProp = value.GetType().GetProperty("Value");
                return valueProp?.GetValue(value)?.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}

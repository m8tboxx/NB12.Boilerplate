using NB12.Boilerplate.BuildingBlocks.Domain.Interfaces;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Ids
{
    public sealed class StronglyTypedIdJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
            => TryGetStronglyTypedIdInfo(typeToConvert, out _, out _);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (!TryGetStronglyTypedIdInfo(typeToConvert, out var valueType, out var valueProp))
                throw new NotSupportedException($"Type '{typeToConvert}' is not a strongly-typed id.");

            var converterType = typeof(StronglyTypedIdJsonConverter<,>).MakeGenericType(typeToConvert, valueType);
            return (JsonConverter)Activator.CreateInstance(converterType, valueProp)!;
        }

        private static bool TryGetStronglyTypedIdInfo(Type candidate, out Type valueType, out PropertyInfo valueProp)
        {
            valueType = null!;
            valueProp = null!;

            // Check if it implements IStronglyTypedId<T>
            var iface = candidate.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStronglyTypedId<>));

            if (iface is null)
                return false;

            valueType = iface.GetGenericArguments()[0];
            valueProp = candidate.GetProperty(nameof(IStronglyTypedId<>.Value))!; // TODO: check if Guid?
            return valueProp is not null;
        }
    }    
}

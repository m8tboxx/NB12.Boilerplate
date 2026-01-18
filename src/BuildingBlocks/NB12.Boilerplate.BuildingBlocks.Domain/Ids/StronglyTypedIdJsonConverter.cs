using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Ids
{
    internal sealed class StronglyTypedIdJsonConverter<TId, TValue> : JsonConverter<TId>
        where TId : class
        where TValue : notnull
    {
        private readonly PropertyInfo _valueProp;

        public StronglyTypedIdJsonConverter(PropertyInfo valueProp) => _valueProp = valueProp;

        public override TId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialize underlying value (Guid/string/int/long...)
            var value = JsonSerializer.Deserialize<TValue>(ref reader, options);

            // We expect a public ctor(TValue value)
            return (TId)Activator.CreateInstance(typeof(TId), value!)!;
        }

        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            var underlying = (TValue)_valueProp.GetValue(value)!;
            JsonSerializer.Serialize(writer, underlying, options);
        }
    }
}

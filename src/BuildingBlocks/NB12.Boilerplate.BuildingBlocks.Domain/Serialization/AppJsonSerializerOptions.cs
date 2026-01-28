using NB12.Boilerplate.BuildingBlocks.Domain.Ids;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Serialization
{
    public static class AppJsonSerializerOptions
    {
        public static JsonSerializerOptions Create()
        {
            var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            Configure(o);
            return o;
        }

        public static void Configure(JsonSerializerOptions o)
        {
            // Serialize Strongly-typed IDs as "Value"
            AddConverterIfMissing(o, new StronglyTypedIdJsonConverterFactory());

            // Enums as strings (camelCase) instead of numbers (API-readable & stable)
            AddConverterIfMissing(o, new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }

        private static void AddConverterIfMissing(JsonSerializerOptions o, JsonConverter converter)
        {
            var t = converter.GetType();
            if (o.Converters.Any(c => c.GetType() == t))
                return;

            o.Converters.Add(converter);
        }
    }
}

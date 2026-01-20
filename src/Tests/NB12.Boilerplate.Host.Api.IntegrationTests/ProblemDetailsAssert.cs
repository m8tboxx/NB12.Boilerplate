using System.Net;
using System.Text.Json;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public static class ProblemDetailsAssert
    {
        public static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
        {
            Assert.Equal(expectedStatus, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // status
            if (root.TryGetProperty("status", out var statusProp) && statusProp.ValueKind == JsonValueKind.Number)
            {
                Assert.Equal((int)expectedStatus, statusProp.GetInt32());
            }

            // type
            Assert.True(root.TryGetProperty("type", out var typeProp), $"ProblemDetails missing 'type'. Body: {json}");
            var type = typeProp.GetString();
            Assert.False(string.IsNullOrWhiteSpace(type), $"ProblemDetails 'type' empty. Body: {json}");

            var code = ExtractCodeFromType(type!);
            Assert.Equal(expectedCode, code);
        }

        private static string ExtractCodeFromType(string type)
        {
            var idx = type.LastIndexOf(':');
            return idx >= 0 && idx < type.Length - 1 ? type[(idx + 1)..] : type;
        }
    }
}

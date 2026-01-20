using System.Net.Http.Json;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public static class HttpClientCookieExtensions
    {
        public static async Task<HttpResponseMessage> PostJsonWithCookiesAsync<T>(
            this HttpClient client,
            CookieJar jar,
            string url,
            T body,
            Action<HttpRequestMessage>? configure = null,
            CancellationToken ct = default)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body)
            };

            jar.ApplyTo(req);
            configure?.Invoke(req);

            var resp = await client.SendAsync(req, ct);
            jar.StoreFrom(resp);
            return resp;
        }

        public static async Task<HttpResponseMessage> PostEmptyWithCookiesAsync(
            this HttpClient client,
            CookieJar jar,
            string url,
            Action<HttpRequestMessage>? configure = null,
            CancellationToken ct = default)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            jar.ApplyTo(req);
            configure?.Invoke(req);

            var resp = await client.SendAsync(req, ct);
            jar.StoreFrom(resp);
            return resp;
        }
    }
}

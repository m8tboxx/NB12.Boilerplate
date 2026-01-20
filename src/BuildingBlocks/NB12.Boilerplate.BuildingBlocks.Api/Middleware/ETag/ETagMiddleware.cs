using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace NB12.Boilerplate.BuildingBlocks.Api.Middleware.ETag
{
    public sealed class ETagMiddleware : IMiddleware
    {
        private readonly ETagOptions _options;

        public ETagMiddleware(IOptions<ETagOptions> options)
            => _options = options.Value;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!IsEligibleRequest(context.Request))
            {
                await next(context);
                return;
            }

            // If response already has ETag, we should not override it.
            // But we only know after endpoint runs.
            var originalBody = context.Response.Body;

            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await next(context);

                if (!IsEligibleResponse(context.Response))
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody, context.RequestAborted);
                    return;
                }

                if (context.Response.Headers.ContainsKey("ETag"))
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody, context.RequestAborted);
                    return;
                }

                if (_options.OnlyForJson && !IsJson(context.Response.ContentType))
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody, context.RequestAborted);
                    return;
                }

                // Avoid hashing huge bodies
                if (buffer.Length == 0 || buffer.Length > _options.MaxBodySizeBytes)
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody, context.RequestAborted);
                    return;
                }

                var bytes = buffer.ToArray();
                var etag = ComputeWeakETag(bytes);

                context.Response.Headers["ETag"] = etag;

                // Helps caches behave correctly when compression is used
                context.Response.Headers.Append("Vary", "Accept-Encoding");

                if (_options.SetCacheControlIfMissing && !context.Response.Headers.ContainsKey("Cache-Control"))
                    context.Response.Headers["Cache-Control"] = _options.CacheControlValue;

                if (RequestMatchesETag(context.Request, etag))
                {
                    // Turn into 304
                    context.Response.StatusCode = StatusCodes.Status304NotModified;

                    // Body must be empty for 304
                    context.Response.ContentLength = 0;
                    context.Response.Headers.Remove("Content-Type");

                    return;
                }

                // Write original body
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody, context.RequestAborted);
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }

        private static bool IsEligibleRequest(HttpRequest request)
            => HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);

        private static bool IsEligibleResponse(HttpResponse response)
            => response.StatusCode == StatusCodes.Status200OK;

        private static bool IsJson(string? contentType)
            => contentType is not null
               && contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);

        private static string ComputeWeakETag(byte[] bytes)
        {
            var hash = SHA256.HashData(bytes);
            // Use base64url-ish to avoid "/" and "+" problems in some proxies/logs
            var b64 = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"W/\"{b64}\"";
        }

        private static bool RequestMatchesETag(HttpRequest request, string etag)
        {
            if (!request.Headers.TryGetValue("If-None-Match", out var inmValues))
                return false;

            var inm = inmValues.ToString();
            if (string.IsNullOrWhiteSpace(inm))
                return false;

            // If-None-Match can contain multiple ETags: W/"a", W/"b"
            // We do a conservative contains check (quoted exact match).
            return inm.Split(',')
                .Select(x => x.Trim())
                .Any(x => string.Equals(x, etag, StringComparison.Ordinal));
        }
    }
}

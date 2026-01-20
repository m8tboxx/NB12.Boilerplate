namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public sealed class CookieJar
    {
        private readonly Dictionary<string, string> _cookies = new(StringComparer.OrdinalIgnoreCase);

        public string? Get(string name)
            => _cookies.TryGetValue(name, out var v) ? v : null;

        public void Set(string name, string value)
            => _cookies[name] = value;

        public void Remove(string name)
            => _cookies.Remove(name);

        public void ApplyTo(HttpRequestMessage request)
        {
            if (_cookies.Count == 0)
                return;

            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(kv => $"{kv.Key}={kv.Value}")));
        }

        public void StoreFrom(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
                return;

            foreach (var header in setCookies)
            {
                // "rt=<value>; path=...; secure; httponly; samesite=strict; expires=..."
                var firstPart = header.Split(';', 2)[0];
                var idx = firstPart.IndexOf('=');
                if (idx <= 0) continue;

                var name = firstPart[..idx].Trim();
                var value = firstPart[(idx + 1)..]; // darf '=' enthalten

                // Delete() liefert i.d.R. leeren Value -> Cookie entfernen
                if (string.IsNullOrEmpty(value))
                    _cookies.Remove(name);
                else
                    _cookies[name] = value;
            }
        }
    }
}

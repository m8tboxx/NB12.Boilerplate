namespace NB12.Boilerplate.BuildingBlocks.Api.Middleware.ETag
{
    public sealed class ETagOptions
    {
        /// <summary>Only apply to JSON responses.</summary>
        public bool OnlyForJson { get; set; } = true;

        /// <summary>Skip hashing and ETag for large bodies (bytes).</summary>
        public int MaxBodySizeBytes { get; set; } = 1_000_000; // 1 MB

        /// <summary>If true, set Cache-Control when not already present.</summary>
        public bool SetCacheControlIfMissing { get; set; } = true;

        /// <summary>Cache-Control value to set (when missing).</summary>
        public string CacheControlValue { get; set; } = "private, max-age=0, must-revalidate";
    }
}

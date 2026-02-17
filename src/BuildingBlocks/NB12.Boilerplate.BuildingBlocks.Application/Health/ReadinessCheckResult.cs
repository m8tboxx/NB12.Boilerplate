namespace NB12.Boilerplate.BuildingBlocks.Application.Health
{
    public readonly record struct ReadinessCheckResult(bool IsHealthy, string? Detail)
    {
        public static ReadinessCheckResult Healthy(string? detail = null) => new(true, detail);
        public static ReadinessCheckResult Unhealthy(string? detail = null) => new(false, detail);
    }
}

namespace NB12.Boilerplate.BuildingBlocks.Application.Health
{
    /// <summary>
    /// Readiness checks decide whether the process should receive production traffic.
    /// Keep checks fast and deterministic (no long-running maintenance work).
    /// </summary>
    public interface IReadinessCheck
    {
        string Name { get; }

        Task<ReadinessCheckResult> CheckAsync(CancellationToken ct);
    }
}

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    /// <summary>
    /// Inbox (consumer-side idempotency) configuration.
    /// </summary>
    public sealed record InboxOptions
    {
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Lock TTL (seconds) for in-flight handler execution.
        /// If a worker crashes mid-handle, the lock expires and allows retry.
        /// </summary>
        public int LockSeconds { get; init; } = 60;
    }
}

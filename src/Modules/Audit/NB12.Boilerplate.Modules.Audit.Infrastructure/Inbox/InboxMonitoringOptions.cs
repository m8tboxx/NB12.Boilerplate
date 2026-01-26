namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox
{
    public sealed record InboxMonitoringOptions
    {
        public bool Enabled { get; init; } = false;

        /// <summary>Poll interval for collecting DB stats.</summary>
        public int PollSeconds { get; init; } = 30;
    }
}

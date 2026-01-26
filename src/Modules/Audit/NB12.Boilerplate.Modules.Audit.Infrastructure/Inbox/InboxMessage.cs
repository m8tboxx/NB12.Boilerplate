namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox
{
    /// <summary>
    /// Inbox record for consumer-side idempotency.
    /// Composite key: (IntegrationEventId, HandlerName)
    /// </summary>
    internal sealed class InboxMessage
    {
        public InboxMessage(
            Guid integrationEventId,
            string handlerName,
            DateTime receivedAtUtc)
        {
            IntegrationEventId = integrationEventId;
            HandlerName = handlerName;
            ReceivedAtUtc = receivedAtUtc;
            AttemptCount = 0;
        }

        // EF needs a parameterless ctor
        private InboxMessage() { }

        public Guid IntegrationEventId { get; private set; }
        public string HandlerName { get; private set; } = null!;

        public DateTime ReceivedAtUtc { get; private set; }

        public int AttemptCount { get; private set; }

        public DateTime? LockedUntilUtc { get; private set; }
        public string? LockedOwner { get; private set; }

        public DateTime? ProcessedAtUtc { get; private set; }
        public string? LastError { get; private set; }
        public DateTime? LastFailedAtUtc { get; private set; }
    }
}

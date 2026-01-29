using NB12.Boilerplate.BuildingBlocks.Application.Ids;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    /// <summary>
    /// Consumer-side idempotency record (Inbox Pattern).
    /// Composite key: (IntegrationEventId, HandlerName)
    /// </summary>
    public sealed class InboxMessage
    {
        public InboxMessage(
            InboxMessageId id,
            Guid integrationEventId,
            string handlerName,
            string eventType,
            string payloadJson,
            DateTime receivedAtUtc)
        {
            Id = id;
            IntegrationEventId = integrationEventId;
            HandlerName = handlerName;
            EventType = eventType;
            PayloadJson = payloadJson;
            ReceivedAtUtc = receivedAtUtc;
            AttemptCount = 0;
        }

        public InboxMessageId Id { get; private set; }
        public Guid IntegrationEventId { get; private set; }
        public string HandlerName { get; private set; } = string.Empty;

        public string EventType { get; private set; } = string.Empty;
        public string PayloadJson { get; private set; } = string.Empty;

        public DateTime ReceivedAtUtc { get; private set; }
        public DateTime? ProcessedAtUtc { get; private set; }

        /// <summary>
        /// Failed attempts count (NOT total tries).
        /// Pending = AttemptCount == 0 and ProcessedAtUtc == null
        /// Failed  = AttemptCount > 0  and ProcessedAtUtc == null
        /// </summary>
        public int AttemptCount { get; private set; }
        public string? LastError { get; private set; }
        public DateTime? LastFailedAtUtc { get; private set; }

        public DateTime? LockedUntilUtc { get; private set; }
        public string? LockedOwner { get; private set; }

        public DateTime? DeadLetteredAtUtc { get; private set; }
        public string? DeadLetterReason { get; private set; }

        public void MarkFailed(string error, DateTime failedAtUtc)
        {
            AttemptCount++;
            LastError = error;
            LastFailedAtUtc = failedAtUtc;
            LockedUntilUtc = null;
            LockedOwner = null;
        }

        public void MarkProcessed(DateTime processedAtUtc)
        {
            ProcessedAtUtc = processedAtUtc;
            LockedUntilUtc = null;
            LockedOwner = null;
            LastError = null;
            LastFailedAtUtc = null;
        }

        public void Lock(string owner, DateTime lockedUntilUtc)
        {
            LockedOwner = owner;
            LockedUntilUtc = lockedUntilUtc;
        }

        public void DeadLetter(string reason, DateTime deadLetterAtUtc)
        {
            DeadLetterReason = reason;
            DeadLetteredAtUtc = deadLetterAtUtc;
        }

        public void ResetForReplay()
        {
            ProcessedAtUtc = null;
            AttemptCount = 0;
            LastError = null;
            LastFailedAtUtc = null;
            LockedUntilUtc = null;
            LockedOwner = null;
            DeadLetteredAtUtc = null;
            DeadLetterReason = null;
        }
    }
}

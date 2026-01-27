using NB12.Boilerplate.Modules.Audit.Domain.Ids;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox
{
    /// <summary>
    /// Inbox record for consumer-side idempotency.
    /// Composite key: (IntegrationEventId, HandlerName)
    /// </summary>
    internal sealed class InboxMessage
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
        public string HandlerName { get; private set; } = string.Empty!;

        public string EventType { get; private set; } = string.Empty; // CLR name or registry name
        public string PayloadJson { get; private set; } = string.Empty;

        public DateTime ReceivedAtUtc { get; private set; }
        public DateTime? ProcessedAtUtc { get; private set; }

        public int AttemptCount { get; private set; }
        public string? LastError { get; private set; }

        public DateTime? LockedUntilUtc { get; private set; }
        public string? LockedOwner { get; private set; }

        public DateTime? DeadLetteredAtUtc { get; private set; }
        public string? DeadLetterReason { get; private set; }


        public void MarkFailed(string error)
        {
            AttemptCount++;
            LastError = error;
        }

        public void MarkProcessed(DateTime processedAtUtc)
        {
            ProcessedAtUtc = processedAtUtc;
            LockedUntilUtc = null;
            LockedOwner = null;
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

            LockedUntilUtc = null;
            LockedOwner = null;

            DeadLetteredAtUtc = null;
            DeadLetterReason = null;
        }
    }
}

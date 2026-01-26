using NB12.Boilerplate.BuildingBlocks.Infrastructure.Ids;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public sealed class OutboxMessage
    {
        public OutboxMessage(
            OutboxMessageId id,
            DateTime occurredAtUtc,
            string type,
            string content,
            int attemptCount,
            DateTime? processedAtUtc,
            string? lastError,
             DateTime? lockedUntilUtc,
            string? lockedBy,
            DateTime? deadLetteredAtUtc,
            string? deadLetterReason)
        {
            Id = id;
            OccurredAtUtc = occurredAtUtc;
            Type = type;
            Content = content;
            AttemptCount = attemptCount;
            ProcessedAtUtc = processedAtUtc;
            LastError = lastError;
            LockedUntilUtc = lockedUntilUtc;
            LockedBy = lockedBy;
            DeadLetteredAtUtc = deadLetteredAtUtc;
            DeadLetterReason = deadLetterReason;
        }

        // Backwards-compatible constructor (NO locking args)
        // -> used by Interceptors that enqueue new messages
        public OutboxMessage(
            OutboxMessageId id,
            DateTime occurredAtUtc,
            string type,
            string content,
            int attemptCount,
            DateTime? processedAtUtc,
            string? lastError)
            : this(
                id,
                occurredAtUtc,
                type,
                content,
                attemptCount,
                processedAtUtc,
                lastError,
                lockedUntilUtc: null,
                lockedBy: null,
                deadLetteredAtUtc: null,
                deadLetterReason: null)
        {
        }

        public void MarkProcessed(DateTime processedAtUtc)
        {
            ProcessedAtUtc = processedAtUtc;
        }

        public void MarkFailed(string lastError)
        {
            AttemptCount += 1;
            LastError = lastError;
        }

        public OutboxMessageId Id { get; private set; }
        public DateTime OccurredAtUtc { get; private set; }

        // Type discriminator for deserialization (e.g. FullName)
        public string Type { get; private set; } = null!;

        // JSON payload
        public string Content { get; private set; } = null!;

        public DateTime? ProcessedAtUtc { get; private set; }
        public int AttemptCount { get; private set; }
        public string? LastError { get; private set; }

        public DateTimeOffset? LockedUntilUtc { get; private set; }
        public string? LockedBy { get; private set; }

        // Dead-letter
        public DateTime? DeadLetteredAtUtc { get; private set; }
        public string? DeadLetterReason { get; private set; }
    }
}

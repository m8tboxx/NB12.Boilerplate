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
            string? lastError
            )
        {
            Id = id;
            OccurredAtUtc = occurredAtUtc;
            Type = type;
            Content = content;
            AttemptCount = attemptCount;
            ProcessedAtUtc = processedAtUtc;
            LastError = lastError;
        }

        public void Proccessed(DateTime processedAtUtc)
        {
            ProcessedAtUtc = processedAtUtc;
        }

        public void Failed(string lastError)
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
    }
}

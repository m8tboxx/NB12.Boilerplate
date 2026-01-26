namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public sealed record OutboxFailurePlan(
        OutboxFailureAction Action,
        DateTime? NextVisibleAtUtc,
        string? DeadLetterReason)
    {
        public static OutboxFailurePlan Retry(DateTime nextVisibleAtUtc)
            => new(OutboxFailureAction.Retry, nextVisibleAtUtc, null);

        public static OutboxFailurePlan DeadLetter(string reason)
            => new(OutboxFailureAction.DeadLetter, null, reason);
    }
}

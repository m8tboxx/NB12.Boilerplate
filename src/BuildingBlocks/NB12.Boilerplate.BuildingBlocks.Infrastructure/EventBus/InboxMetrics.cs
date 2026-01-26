using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using System.Diagnostics.Metrics;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus
{
    public sealed class InboxMetrics : IDisposable
    {
        public const string MeterName = "NB12.Boilerplate.Inbox";

        private readonly Meter _meter;
        private readonly IInboxStatsProvider _stats;

        private readonly Counter<long> _handlerAcquireTotal;
        private readonly Counter<long> _handlerDuplicateSkipTotal;
        private readonly Counter<long> _handlerProcessedTotal;
        private readonly Counter<long> _handlerFailedTotal;
        private readonly Histogram<double> _handlerDurationMs;

        private readonly Counter<long> _cleanupRunsTotal;
        private readonly Counter<long> _cleanupDeletedProcessedTotal;
        private readonly Counter<long> _cleanupDeletedFailedTotal;
        private readonly Histogram<double> _cleanupDurationMs;

        public InboxMetrics(IInboxStatsProvider stats)
        {
            _stats = stats;

            _meter = new Meter(MeterName);

            _handlerAcquireTotal = _meter.CreateCounter<long>(
                name: "inbox_handler_acquire_total",
                unit: "1",
                description: "Number of inbox acquisitions (handler execution attempts).");

            _handlerDuplicateSkipTotal = _meter.CreateCounter<long>(
                name: "inbox_handler_duplicate_skip_total",
                unit: "1",
                description: "Number of skipped handler executions due to inbox idempotency (already processed or locked).");

            _handlerProcessedTotal = _meter.CreateCounter<long>(
                name: "inbox_handler_processed_total",
                unit: "1",
                description: "Number of successfully processed handler executions.");

            _handlerFailedTotal = _meter.CreateCounter<long>(
                name: "inbox_handler_failed_total",
                unit: "1",
                description: "Number of failed handler executions.");

            _handlerDurationMs = _meter.CreateHistogram<double>(
                name: "inbox_handler_duration_ms",
                unit: "ms",
                description: "Duration of integration event handler execution.");

            _cleanupRunsTotal = _meter.CreateCounter<long>(
                name: "inbox_cleanup_runs_total",
                unit: "1",
                description: "Number of inbox cleanup runs.");

            _cleanupDeletedProcessedTotal = _meter.CreateCounter<long>(
                name: "inbox_cleanup_deleted_processed_total",
                unit: "1",
                description: "Number of deleted processed inbox rows.");

            _cleanupDeletedFailedTotal = _meter.CreateCounter<long>(
                name: "inbox_cleanup_deleted_failed_total",
                unit: "1",
                description: "Number of deleted failed inbox rows.");

            _cleanupDurationMs = _meter.CreateHistogram<double>(
                name: "inbox_cleanup_duration_ms",
                unit: "ms",
                description: "Duration of inbox cleanup run.");

            _meter.CreateObservableGauge<long>(
                name: "inbox_db_total",
                observeValue: () => _stats.GetSnapshot().Total,
                unit: "1",
                description: "Total number of inbox rows in the DB.");

            _meter.CreateObservableGauge<long>(
                name: "inbox_db_pending",
                observeValue: () => _stats.GetSnapshot().Pending,
                unit: "1",
                description: "Number of pending inbox rows (not processed yet).");

            _meter.CreateObservableGauge<long>(
                name: "inbox_db_processed",
                observeValue: () => _stats.GetSnapshot().Processed,
                unit: "1",
                description: "Number of processed inbox rows.");

            _meter.CreateObservableGauge<long>(
                name: "inbox_db_failed",
                observeValue: () => _stats.GetSnapshot().Failed,
                unit: "1",
                description: "Number of failed inbox rows (unprocessed with a failure recorded).");

            _meter.CreateObservableGauge<long>(
                name: "inbox_db_locked",
                observeValue: () => _stats.GetSnapshot().Locked,
                unit: "1",
                description: "Number of locked inbox rows (in-flight handling).");

            _meter.CreateObservableGauge<double>(
                name: "inbox_db_stats_age_seconds",
                observeValue: () =>
                {
                    var snap = _stats.GetSnapshot();
                    if (snap.LastUpdatedUtc == DateTime.MinValue)
                        return double.NaN;

                    var age = DateTime.UtcNow - snap.LastUpdatedUtc;
                    return age.TotalSeconds;
                },
                unit: "s",
                description: "Age of the last inbox stats collection snapshot (seconds).");
        }

        public void HandlerAcquire(string handlerName)
            => _handlerAcquireTotal.Add(1, new KeyValuePair<string, object?>("handler", handlerName));

        public void HandlerDuplicateSkip(string handlerName)
            => _handlerDuplicateSkipTotal.Add(1, new KeyValuePair<string, object?>("handler", handlerName));

        public void HandlerProcessed(string handlerName)
            => _handlerProcessedTotal.Add(1, new KeyValuePair<string, object?>("handler", handlerName));

        public void HandlerFailed(string handlerName)
            => _handlerFailedTotal.Add(1, new KeyValuePair<string, object?>("handler", handlerName));

        public void HandlerDuration(string handlerName, double durationMs)
            => _handlerDurationMs.Record(durationMs, new KeyValuePair<string, object?>("handler", handlerName));

        public void CleanupRun(double durationMs, long deletedProcessed, long deletedFailed)
        {
            _cleanupRunsTotal.Add(1);
            _cleanupDurationMs.Record(durationMs);
            if (deletedProcessed > 0) _cleanupDeletedProcessedTotal.Add(deletedProcessed);
            if (deletedFailed > 0) _cleanupDeletedFailedTotal.Add(deletedFailed);
        }

        public void Dispose()
        {
            _meter.Dispose();
        }
    }
}

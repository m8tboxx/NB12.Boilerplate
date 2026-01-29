using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public sealed class InboxStoreLockingTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public InboxStoreLockingTests(CustomWebApplicationFactory factory)
            => _factory = factory;

        [Fact]
        public async Task Inbox_store_enforces_lock_retry_and_processed_semantics()
        {
            using var scope = _factory.Services.CreateScope();

            // Audit ist garantiert im Test-Host vorhanden -> Key muss exakt so registriert sein
            var inbox = scope.ServiceProvider.GetRequiredKeyedService<IInboxStore>("Audit");

            var id = Guid.NewGuid();
            const string handler = "Test.Handler";
            const string type = "Test.EventType";
            const string payload = "{\"x\":1}";

            var now = DateTime.UtcNow;

            var acquired1 = await inbox.TryAcquireAsync(
                integrationEventId: id,
                handlerName: handler,
                lockOwner: "owner-1",
                utcNow: now,
                lockedUntilUtc: now.AddSeconds(60),
                eventType: type,
                payloadJson: payload,
                ct: CancellationToken.None);

            Assert.True(acquired1);

            var acquired2 = await inbox.TryAcquireAsync(
                integrationEventId: id,
                handlerName: handler,
                lockOwner: "owner-2",
                utcNow: now,
                lockedUntilUtc: now.AddSeconds(60),
                eventType: type,
                payloadJson: payload,
                ct: CancellationToken.None);

            Assert.False(acquired2);

            await inbox.MarkFailedAsync(
                integrationEventId: id,
                handlerName: handler,
                lockOwner: "owner-1",
                failedAtUtc: DateTime.UtcNow,
                error: "boom",
                ct: CancellationToken.None);

            var acquired3 = await inbox.TryAcquireAsync(
                integrationEventId: id,
                handlerName: handler,
                lockOwner: "owner-2",
                utcNow: DateTime.UtcNow,
                lockedUntilUtc: DateTime.UtcNow.AddSeconds(60),
                eventType: type,
                payloadJson: payload,
                ct: CancellationToken.None);

            Assert.True(acquired3);

            await inbox.MarkProcessedAsync(
                integrationEventId: id,
                handlerName: handler,
                lockOwner: "owner-2",
                processedAtUtc: DateTime.UtcNow,
                ct: CancellationToken.None);

            var acquired4 = await inbox.TryAcquireAsync(
                integrationEventId: id,
                handlerName: handler,
                lockOwner: "owner-3",
                utcNow: DateTime.UtcNow,
                lockedUntilUtc: DateTime.UtcNow.AddSeconds(60),
                eventType: type,
                payloadJson: payload,
                ct: CancellationToken.None);

            Assert.False(acquired4);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InboxImplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_IntegrationEventId",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "audit",
                columns: table => new
                {
                    IntegrationEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlerName = table.Column<string>(type: "text", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedOwner = table.Column<string>(type: "text", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    LastFailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => new { x.IntegrationEventId, x.HandlerName });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IntegrationEventId",
                schema: "audit",
                table: "AuditLogs",
                column: "IntegrationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_LockedUntilUtc",
                schema: "audit",
                table: "InboxMessages",
                column: "LockedUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ProcessedAtUtc",
                schema: "audit",
                table: "InboxMessages",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ReceivedAtUtc",
                schema: "audit",
                table: "InboxMessages",
                column: "ReceivedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "audit");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_IntegrationEventId",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IntegrationEventId",
                schema: "audit",
                table: "AuditLogs",
                column: "IntegrationEventId",
                unique: true);
        }
    }
}

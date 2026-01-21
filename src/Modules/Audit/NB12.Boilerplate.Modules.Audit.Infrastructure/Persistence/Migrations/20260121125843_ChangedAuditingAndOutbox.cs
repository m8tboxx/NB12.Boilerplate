using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangedAuditingAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "audit");

            migrationBuilder.AddColumn<Guid>(
                name: "IntegrationEventId",
                schema: "audit",
                table: "AuditLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Module",
                schema: "audit",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IntegrationEventId",
                schema: "audit",
                table: "AuditLogs",
                column: "IntegrationEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Module_EntityType_EntityId",
                schema: "audit",
                table: "AuditLogs",
                columns: new[] { "Module", "EntityType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_IntegrationEventId",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Module_EntityType_EntityId",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IntegrationEventId",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Module",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc",
                schema: "audit",
                table: "OutboxMessages",
                column: "ProcessedAtUtc");
        }
    }
}

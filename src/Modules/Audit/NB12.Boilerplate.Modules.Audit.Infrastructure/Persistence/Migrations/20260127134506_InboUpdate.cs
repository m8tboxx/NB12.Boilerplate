using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InboUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InboxMessages",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.RenameColumn(
                name: "LastFailedAtUtc",
                schema: "audit",
                table: "InboxMessages",
                newName: "DeadLetteredAtUtc");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                schema: "audit",
                table: "InboxMessages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DeadLetterReason",
                schema: "audit",
                table: "InboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                schema: "audit",
                table: "InboxMessages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PayloadJson",
                schema: "audit",
                table: "InboxMessages",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InboxMessages",
                schema: "audit",
                table: "InboxMessages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_AttemptCount",
                schema: "audit",
                table: "InboxMessages",
                column: "AttemptCount");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_HandlerName",
                schema: "audit",
                table: "InboxMessages",
                column: "HandlerName");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_IntegrationEventId",
                schema: "audit",
                table: "InboxMessages",
                column: "IntegrationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_IntegrationEventId_HandlerName",
                schema: "audit",
                table: "InboxMessages",
                columns: new[] { "IntegrationEventId", "HandlerName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InboxMessages",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_AttemptCount",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_HandlerName",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_IntegrationEventId",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_IntegrationEventId_HandlerName",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "DeadLetterReason",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "EventType",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "PayloadJson",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.RenameColumn(
                name: "DeadLetteredAtUtc",
                schema: "audit",
                table: "InboxMessages",
                newName: "LastFailedAtUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InboxMessages",
                schema: "audit",
                table: "InboxMessages",
                columns: new[] { "IntegrationEventId", "HandlerName" });
        }
    }
}

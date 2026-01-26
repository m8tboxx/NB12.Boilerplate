using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutboxDeadLetter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_LockedUntilUtc",
                schema: "auth",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<string>(
                name: "DeadLetterReason",
                schema: "auth",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAtUtc",
                schema: "auth",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DeadLetteredAtUtc",
                schema: "auth",
                table: "OutboxMessages",
                column: "DeadLetteredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_DeadLetteredAtUtc_LockedUntil~",
                schema: "auth",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "DeadLetteredAtUtc", "LockedUntilUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_DeadLetteredAtUtc",
                schema: "auth",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_DeadLetteredAtUtc_LockedUntil~",
                schema: "auth",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeadLetterReason",
                schema: "auth",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAtUtc",
                schema: "auth",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_LockedUntilUtc",
                schema: "auth",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "LockedUntilUtc" });
        }
    }
}

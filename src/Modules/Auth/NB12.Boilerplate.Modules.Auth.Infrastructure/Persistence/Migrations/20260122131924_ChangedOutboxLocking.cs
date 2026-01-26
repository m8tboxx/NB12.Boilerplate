using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangedOutboxLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                schema: "auth",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedUntilUtc",
                schema: "auth",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_LockedUntilUtc",
                schema: "auth",
                table: "OutboxMessages",
                column: "LockedUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_LockedUntilUtc",
                schema: "auth",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "LockedUntilUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_LockedUntilUtc",
                schema: "auth",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_LockedUntilUtc",
                schema: "auth",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                schema: "auth",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                schema: "auth",
                table: "OutboxMessages");
        }
    }
}

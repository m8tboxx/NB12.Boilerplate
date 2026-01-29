using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CodeRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedAtUtc",
                schema: "audit",
                table: "InboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_LastFailedAtUtc",
                schema: "audit",
                table: "InboxMessages",
                column: "LastFailedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_LastFailedAtUtc",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "LastFailedAtUtc",
                schema: "audit",
                table: "InboxMessages");
        }
    }
}

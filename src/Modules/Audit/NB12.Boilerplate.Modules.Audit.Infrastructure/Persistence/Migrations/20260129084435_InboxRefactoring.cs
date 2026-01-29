using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InboxRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_LastFailedAtUtc",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_DeadLetteredAtUtc",
                schema: "audit",
                table: "InboxMessages",
                column: "DeadLetteredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_DeadLetteredAtUtc",
                schema: "audit",
                table: "InboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_LastFailedAtUtc",
                schema: "audit",
                table: "InboxMessages",
                column: "LastFailedAtUtc");
        }
    }
}

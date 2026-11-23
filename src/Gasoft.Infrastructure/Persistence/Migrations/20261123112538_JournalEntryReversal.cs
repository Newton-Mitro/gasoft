using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class JournalEntryReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReversalOfEntryId",
                table: "JournalEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_JournalEntries_TenantId_CompanyId_ReversalOfEntryId",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CompanyId", "ReversalOfEntryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_JournalEntries_TenantId_CompanyId_ReversalOf~",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CompanyId", "ReversalOfEntryId" },
                principalTable: "JournalEntries",
                principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_JournalEntries_TenantId_CompanyId_ReversalOf~",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "UX_JournalEntries_TenantId_CompanyId_ReversalOfEntryId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ReversalOfEntryId",
                table: "JournalEntries");
        }
    }
}

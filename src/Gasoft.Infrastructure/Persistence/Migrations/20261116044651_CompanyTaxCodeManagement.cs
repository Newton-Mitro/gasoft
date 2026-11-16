using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompanyTaxCodeManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxCodes_JurisdictionId_Code",
                table: "TaxCodes");

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "TaxCodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "TaxCodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_JurisdictionId",
                table: "TaxCodes",
                column: "JurisdictionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_TenantId_CompanyId_JurisdictionId_Code",
                table: "TaxCodes",
                columns: new[] { "TenantId", "CompanyId", "JurisdictionId", "Code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxCodes_CompanyScope",
                table: "TaxCodes",
                sql: "(\"TenantId\" IS NULL AND \"CompanyId\" IS NULL) OR (\"TenantId\" IS NOT NULL AND \"CompanyId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxCodes_Companies_TenantId_CompanyId",
                table: "TaxCodes",
                columns: new[] { "TenantId", "CompanyId" },
                principalTable: "Companies",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                INSERT INTO "TaxCategories" ("Code", "Name")
                VALUES
                    ('STANDARD', 'Standard'),
                    ('REDUCED', 'Reduced'),
                    ('ZERO', 'Zero-rated'),
                    ('EXEMPT', 'Exempt'),
                    ('REVERSE_CHARGE', 'Reverse charge')
                ON CONFLICT ("Code") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxCodes_Companies_TenantId_CompanyId",
                table: "TaxCodes");

            migrationBuilder.DropIndex(
                name: "IX_TaxCodes_JurisdictionId",
                table: "TaxCodes");

            migrationBuilder.DropIndex(
                name: "IX_TaxCodes_TenantId_CompanyId_JurisdictionId_Code",
                table: "TaxCodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxCodes_CompanyScope",
                table: "TaxCodes");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "TaxCodes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TaxCodes");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_JurisdictionId_Code",
                table: "TaxCodes",
                columns: new[] { "JurisdictionId", "Code" },
                unique: true);
        }
    }
}

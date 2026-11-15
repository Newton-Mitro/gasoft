using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "TenantUsers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Code", "DecimalPlaces", "Name" },
                values: new object[,]
                {
                    { "AUD", (byte)2, "Australian Dollar" },
                    { "BDT", (byte)2, "Bangladeshi Taka" },
                    { "CAD", (byte)2, "Canadian Dollar" },
                    { "CHF", (byte)2, "Swiss Franc" },
                    { "EUR", (byte)2, "Euro" },
                    { "GBP", (byte)2, "Pound Sterling" },
                    { "INR", (byte)2, "Indian Rupee" },
                    { "JPY", (byte)0, "Japanese Yen" },
                    { "SGD", (byte)2, "Singapore Dollar" },
                    { "USD", (byte)2, "US Dollar" }
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Code",
                keyValue: "AUD");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Code",
                keyValue: "BDT");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Code",
                keyValue: "CAD");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Code",
                keyValue: "CHF");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Code",
                keyValue: "EUR");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Code",
                keyValue: "GBP");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Code",
                keyValue: "INR");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Code",
                keyValue: "JPY");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Code",
                keyValue: "SGD");

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumn: "Code",
                keyValue: "USD");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "TenantUsers");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesQuoteEmailDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SentAt",
                table: "SalesQuotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SentTo",
                table: "SalesQuotes",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "SalesQuotes");

            migrationBuilder.DropColumn(
                name: "SentTo",
                table: "SalesQuotes");
        }
    }
}

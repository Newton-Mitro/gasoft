using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificationEmailOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Notifications_TenantId_Id",
                table: "Notifications",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "NotificationEmailOutbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotificationId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationEmailOutbox", x => x.Id);
                    table.CheckConstraint("CK_NotificationEmailOutbox_Attempts", "\"AttemptCount\" >= 0");
                    table.CheckConstraint("CK_NotificationEmailOutbox_Status", "\"Status\" IN ('Pending', 'Processing', 'Sent', 'Failed')");
                    table.ForeignKey(
                        name: "FK_NotificationEmailOutbox_Notifications_TenantId_Notification~",
                        columns: x => new { x.TenantId, x.NotificationId },
                        principalTable: "Notifications",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEmailOutbox_Status_NextAttemptAt_LeaseExpiresAt",
                table: "NotificationEmailOutbox",
                columns: new[] { "Status", "NextAttemptAt", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEmailOutbox_TenantId_NotificationId",
                table: "NotificationEmailOutbox",
                columns: new[] { "TenantId", "NotificationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationEmailOutbox");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Notifications_TenantId_Id",
                table: "Notifications");
        }
    }
}

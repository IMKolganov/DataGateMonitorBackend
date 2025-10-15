using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class Notification_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    DedupKey = table.Column<string>(type: "text", nullable: true),
                    ServerId = table.Column<int>(type: "integer", nullable: true),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    RelatedClientId = table.Column<int>(type: "integer", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRecipients",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotificationId = table.Column<int>(type: "integer", nullable: false),
                    AdminUserId = table.Column<int>(type: "integer", nullable: false),
                    DeliveryChannel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveryStatus = table.Column<int>(type: "integer", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRecipients_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "xgb_dashopnvpn",
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipient_AdminUserId",
                schema: "xgb_dashopnvpn",
                table: "NotificationRecipients",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipient_AdminUserId_DeliveryStatus",
                schema: "xgb_dashopnvpn",
                table: "NotificationRecipients",
                columns: new[] { "AdminUserId", "DeliveryStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipient_NotificationId",
                schema: "xgb_dashopnvpn",
                table: "NotificationRecipients",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "UX_NotificationRecipient_NotificationId_AdminUserId_DeliveryChannel",
                schema: "xgb_dashopnvpn",
                table: "NotificationRecipients",
                columns: new[] { "NotificationId", "AdminUserId", "DeliveryChannel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_ActorUserId",
                schema: "xgb_dashopnvpn",
                table: "Notifications",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_ServerId",
                schema: "xgb_dashopnvpn",
                table: "Notifications",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_Severity",
                schema: "xgb_dashopnvpn",
                table: "Notifications",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_Type",
                schema: "xgb_dashopnvpn",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_Type_ServerId_DedupKey",
                schema: "xgb_dashopnvpn",
                table: "Notifications",
                columns: new[] { "Type", "ServerId", "DedupKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationRecipients",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "xgb_dashopnvpn");
        }
    }
}

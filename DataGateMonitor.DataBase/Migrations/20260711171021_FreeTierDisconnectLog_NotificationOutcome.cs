using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class FreeTierDisconnectLog_NotificationOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationChannel",
                schema: "xgb_dashopnvpn",
                table: "FreeTierDisconnectLogs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotificationSent",
                schema: "xgb_dashopnvpn",
                table: "FreeTierDisconnectLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationChannel",
                schema: "xgb_dashopnvpn",
                table: "FreeTierDisconnectLogs");

            migrationBuilder.DropColumn(
                name: "NotificationSent",
                schema: "xgb_dashopnvpn",
                table: "FreeTierDisconnectLogs");
        }
    }
}

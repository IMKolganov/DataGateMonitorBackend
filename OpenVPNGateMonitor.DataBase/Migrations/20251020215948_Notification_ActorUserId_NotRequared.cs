using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class Notification_ActorUserId_NotRequared : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ActorUserId",
                schema: "xgb_dashopnvpn",
                table: "Notifications",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ActorUserId",
                schema: "xgb_dashopnvpn",
                table: "Notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}

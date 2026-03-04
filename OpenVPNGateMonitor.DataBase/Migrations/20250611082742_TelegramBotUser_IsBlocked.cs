using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class TelegramBotUser_IsBlocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                schema: "xgb_dashopnvpn",
                table: "TelegramBotUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBlocked",
                schema: "xgb_dashopnvpn",
                table: "TelegramBotUsers");
        }
    }
}
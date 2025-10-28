using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class TelegramBotUser_LanguageCode_IsPremium_Add : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                schema: "xgb_dashopnvpn",
                table: "TelegramBotUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                schema: "xgb_dashopnvpn",
                table: "TelegramBotUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPremium",
                schema: "xgb_dashopnvpn",
                table: "TelegramBotUsers");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                schema: "xgb_dashopnvpn",
                table: "TelegramBotUsers");
        }
    }
}

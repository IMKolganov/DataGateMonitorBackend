using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class UserEmailConfirmationInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailConfirmed",
                schema: "xgb_dashopnvpn",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmailConfirmed",
                schema: "xgb_dashopnvpn",
                table: "Users");
        }
    }
}

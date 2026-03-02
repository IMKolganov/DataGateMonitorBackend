using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServer_IsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServers_IsDeleted",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServers_IsDeleted",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");
        }
    }
}

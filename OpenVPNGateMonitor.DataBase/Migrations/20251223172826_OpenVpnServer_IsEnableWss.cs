using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServer_IsEnableWss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnableWss",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsEnableWss",
                value: false);

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsEnableWss",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServers_IsEnableWss",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                column: "IsEnableWss");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServers_IsEnableWss",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropColumn(
                name: "IsEnableWss",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");
        }
    }
}

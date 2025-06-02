using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServer_RemoveLegacyColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Login",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropColumn(
                name: "ManagementIp",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropColumn(
                name: "ManagementPort",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropColumn(
                name: "Password",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Login",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ManagementIp",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ManagementPort",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Login", "ManagementIp", "ManagementPort", "Password" },
                values: new object[] { "", "openvpn_udp", 5092, "" });

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Login", "ManagementIp", "ManagementPort", "Password" },
                values: new object[] { "", "openvpn_tcp", 5093, "" });
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServer_ApiUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiUrl",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                keyColumn: "Id",
                keyValue: 1,
                column: "ApiUrl",
                value: "http://openvpn_udp:5000/");

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                keyColumn: "Id",
                keyValue: 2,
                column: "ApiUrl",
                value: "http://openvpn_tcp:5000/");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiUrl",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");
        }
    }
}
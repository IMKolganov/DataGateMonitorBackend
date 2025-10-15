using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerSeedData_Seed_Url : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                keyColumn: "Id",
                keyValue: 1,
                column: "ApiUrl",
                value: "http://openvpn_udp:5010/");

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                keyColumn: "Id",
                keyValue: 2,
                column: "ApiUrl",
                value: "http://openvpn_tcp:5011/");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
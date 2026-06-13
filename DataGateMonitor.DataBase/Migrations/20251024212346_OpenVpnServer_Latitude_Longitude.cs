using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServer_Latitude_Longitude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDisable",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                type: "double precision",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                type: "double precision",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsDisable", "Latitude", "Longitude" },
                values: new object[] { false, 35.185600000000001, 33.382300000000001 });

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsDisable", "Latitude", "Longitude" },
                values: new object[] { false, 55.755800000000001, 37.6173 });

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServers_IsDefault",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServers_IsDisable",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                column: "IsDisable");

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServers_IsOnline",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServers_Latitude_Longitude",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServers_ServerName",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                column: "ServerName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServers_IsDefault",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServers_IsDisable",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServers_IsOnline",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServers_Latitude_Longitude",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServers_ServerName",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropColumn(
                name: "IsDisable",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class VpnServer_XrayClientsPollMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "XrayClientsPollError",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "XrayClientsPolledAt",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "XrayClientsPollError", "XrayClientsPolledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "XrayClientsPollError", "XrayClientsPolledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "XrayClientsPollError", "XrayClientsPolledAt" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "XrayClientsPollError",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropColumn(
                name: "XrayClientsPolledAt",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");
        }
    }
}

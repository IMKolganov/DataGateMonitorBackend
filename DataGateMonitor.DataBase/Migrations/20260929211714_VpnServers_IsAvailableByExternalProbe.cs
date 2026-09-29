using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class VpnServers_IsAvailableByExternalProbe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExternalProbeCheckedAtUtc",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProbeSummary",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableByExternalProbe",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_VpnServers_IsAvailableByExternalProbe",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                column: "IsAvailableByExternalProbe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VpnServers_IsAvailableByExternalProbe",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropColumn(
                name: "ExternalProbeCheckedAtUtc",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropColumn(
                name: "ExternalProbeSummary",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropColumn(
                name: "IsAvailableByExternalProbe",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");
        }
    }
}

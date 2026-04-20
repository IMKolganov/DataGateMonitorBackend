using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class SeedVpnServerXrayAndQuotaLink : Migration
    {
        private static readonly DateTimeOffset SeedTimestamp = new(
            new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
            TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                columns: new[] { "Id", "ApiUrl", "DcoIsEnabled", "IsDefault", "IsDisable", "IsEnableWss", "IsOnline", "Latitude", "Longitude", "ServerName", "ServerType" },
                values: new object[] { 3, "http://xray:5010/", null, false, false, false, false, 52.367600000000003, 4.9040999999999997, "Xray Server (VLESS)", 1 });

            // Reserved Id for seed row (2, 3); avoids raw SQL while keeping a stable surrogate Id column.
            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "QuotaPlanAllowedServers",
                columns: new[] { "QuotaPlanId", "VpnServerId", "Id", "CreateDate", "LastUpdate" },
                values: new object[] { 2, 3, 910003, SeedTimestamp, SeedTimestamp });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "QuotaPlanAllowedServers",
                keyColumns: new[] { "QuotaPlanId", "VpnServerId" },
                keyValues: new object[] { 2, 3 });

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}

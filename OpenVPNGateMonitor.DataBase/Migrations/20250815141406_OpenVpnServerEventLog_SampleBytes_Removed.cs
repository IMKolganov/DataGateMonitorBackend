using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerEventLog_SampleBytes_Removed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SampleBytesIn",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "SampleBytesOut",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SampleBytesIn",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SampleBytesOut",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "bigint",
                nullable: true);
        }
    }
}

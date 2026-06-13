using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class ClientTraffic_At_Covering_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClientTraffic_At_Session",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClientTraffics");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTraffic_At_Covering",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClientTraffics",
                columns: new[] { "MeasuredAt", "SessionId" })
                .Annotation("Npgsql:IndexInclude", new[] { "ExternalId", "VpnServerId", "BytesReceived", "BytesSent" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClientTraffic_At_Covering",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClientTraffics");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTraffic_At_Session",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClientTraffics",
                columns: new[] { "MeasuredAt", "SessionId" });
        }
    }
}

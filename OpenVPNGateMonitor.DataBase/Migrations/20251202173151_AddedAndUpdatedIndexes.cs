using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class AddedAndUpdatedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ClientTraffic_Server_Session_At",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics");

            migrationBuilder.CreateIndex(
                name: "IX_ServerStatusLogs_Server",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerStatusLogs",
                column: "VpnServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerStatusLogs_Server_Id",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerStatusLogs",
                columns: new[] { "VpnServerId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ServerStatusLogs_Server_Session",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerStatusLogs",
                columns: new[] { "VpnServerId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTraffic_At",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics",
                column: "MeasuredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTraffic_At_External_Session",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics",
                columns: new[] { "MeasuredAt", "ExternalId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTraffic_At_Session",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics",
                columns: new[] { "MeasuredAt", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "UX_ClientTraffic_Server_Session_At",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics",
                columns: new[] { "VpnServerId", "MeasuredAt", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServerClients_ConnectedSince_ExternalId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients",
                columns: new[] { "ConnectedSince", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServerClients_ConnectedSince_Lat_Lon",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients",
                columns: new[] { "ConnectedSince", "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServerClients_Server_IsConnected",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients",
                columns: new[] { "VpnServerId", "IsConnected" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServerClients_Server_IsConnected_Session",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients",
                columns: new[] { "VpnServerId", "IsConnected", "SessionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServerStatusLogs_Server",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerStatusLogs");

            migrationBuilder.DropIndex(
                name: "IX_ServerStatusLogs_Server_Id",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerStatusLogs");

            migrationBuilder.DropIndex(
                name: "IX_ServerStatusLogs_Server_Session",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerStatusLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClientTraffic_At",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics");

            migrationBuilder.DropIndex(
                name: "IX_ClientTraffic_At_External_Session",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics");

            migrationBuilder.DropIndex(
                name: "IX_ClientTraffic_At_Session",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics");

            migrationBuilder.DropIndex(
                name: "UX_ClientTraffic_Server_Session_At",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics");

            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServerClients_ConnectedSince_ExternalId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients");

            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServerClients_ConnectedSince_Lat_Lon",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients");

            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServerClients_Server_IsConnected",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients");

            migrationBuilder.DropIndex(
                name: "IX_OpenVpnServerClients_Server_IsConnected_Session",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients");

            migrationBuilder.CreateIndex(
                name: "UX_ClientTraffic_Server_Session_At",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics",
                columns: new[] { "VpnServerId", "SessionId", "MeasuredAt" },
                unique: true);
        }
    }
}

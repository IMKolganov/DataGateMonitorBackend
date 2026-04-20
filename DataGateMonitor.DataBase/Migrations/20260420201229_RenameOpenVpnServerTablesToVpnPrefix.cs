using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class RenameOpenVpnServerTablesToVpnPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "OpenVpnServerTags",
                schema: "xgb_dashopnvpn",
                newName: "VpnServerTags",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "OpenVpnServerStatusLogs",
                schema: "xgb_dashopnvpn",
                newName: "VpnServerStatusLogs",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "OpenVpnServerOvpnFileConfigs",
                schema: "xgb_dashopnvpn",
                newName: "VpnServerOvpnFileConfigs",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "OpenVpnServerEventLogs",
                schema: "xgb_dashopnvpn",
                newName: "VpnServerEventLogs",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "OpenVpnServerConflogs",
                schema: "xgb_dashopnvpn",
                newName: "VpnServerConflogs",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "OpenVpnServerClientTraffics",
                schema: "xgb_dashopnvpn",
                newName: "VpnServerClientTraffics",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "OpenVpnServerClients",
                schema: "xgb_dashopnvpn",
                newName: "VpnServerClients",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServerTags_VpnServerId",
                schema: "xgb_dashopnvpn",
                table: "VpnServerTags",
                newName: "IX_VpnServerTags_VpnServerId");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServerTags_TagId",
                schema: "xgb_dashopnvpn",
                table: "VpnServerTags",
                newName: "IX_VpnServerTags_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServerConflogs_VpnServerId",
                schema: "xgb_dashopnvpn",
                table: "VpnServerConflogs",
                newName: "IX_VpnServerConflogs_VpnServerId");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServerConflogs_RequestUrl",
                schema: "xgb_dashopnvpn",
                table: "VpnServerConflogs",
                newName: "IX_VpnServerConflogs_RequestUrl");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServerConflogs_CreateDate",
                schema: "xgb_dashopnvpn",
                table: "VpnServerConflogs",
                newName: "IX_VpnServerConflogs_CreateDate");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServerClients_ConnectedSince_ExternalId",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClients",
                newName: "IX_VpnServerClients_ConnectedSince_ExternalId");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServerClients_Server_IsConnected",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClients",
                newName: "IX_VpnServerClients_Server_IsConnected");

            migrationBuilder.RenameIndex(
                name: "UX_OpenVpnServerClients_Server_Session",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClients",
                newName: "UX_VpnServerClients_Server_Session");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServerClients_ConnectedSince_Lat_Lon",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClients",
                newName: "IX_VpnServerClients_ConnectedSince_Lat_Lon");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServerClients_Server_IsConnected_Session",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClients",
                newName: "IX_VpnServerClients_Server_IsConnected_Session");

            migrationBuilder.Sql(
                """
                ALTER TABLE xgb_dashopnvpn."VpnServerTags" RENAME CONSTRAINT "PK_OpenVpnServerTags" TO "PK_VpnServerTags";
                ALTER TABLE xgb_dashopnvpn."VpnServerStatusLogs" RENAME CONSTRAINT "PK_OpenVpnServerStatusLogs" TO "PK_VpnServerStatusLogs";
                ALTER TABLE xgb_dashopnvpn."VpnServerOvpnFileConfigs" RENAME CONSTRAINT "PK_OpenVpnServerOvpnFileConfigs" TO "PK_VpnServerOvpnFileConfigs";
                ALTER TABLE xgb_dashopnvpn."VpnServerEventLogs" RENAME CONSTRAINT "PK_OpenVpnServerEventLogs" TO "PK_VpnServerEventLogs";
                ALTER TABLE xgb_dashopnvpn."VpnServerConflogs" RENAME CONSTRAINT "PK_OpenVpnServerConflogs" TO "PK_VpnServerConflogs";
                ALTER TABLE xgb_dashopnvpn."VpnServerClientTraffics" RENAME CONSTRAINT "PK_OpenVpnServerClientTraffics" TO "PK_VpnServerClientTraffics";
                ALTER TABLE xgb_dashopnvpn."VpnServerClients" RENAME CONSTRAINT "PK_OpenVpnServerClients" TO "PK_VpnServerClients";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE xgb_dashopnvpn."VpnServerTags" RENAME CONSTRAINT "PK_VpnServerTags" TO "PK_OpenVpnServerTags";
                ALTER TABLE xgb_dashopnvpn."VpnServerStatusLogs" RENAME CONSTRAINT "PK_VpnServerStatusLogs" TO "PK_OpenVpnServerStatusLogs";
                ALTER TABLE xgb_dashopnvpn."VpnServerOvpnFileConfigs" RENAME CONSTRAINT "PK_VpnServerOvpnFileConfigs" TO "PK_OpenVpnServerOvpnFileConfigs";
                ALTER TABLE xgb_dashopnvpn."VpnServerEventLogs" RENAME CONSTRAINT "PK_VpnServerEventLogs" TO "PK_OpenVpnServerEventLogs";
                ALTER TABLE xgb_dashopnvpn."VpnServerConflogs" RENAME CONSTRAINT "PK_VpnServerConflogs" TO "PK_OpenVpnServerConflogs";
                ALTER TABLE xgb_dashopnvpn."VpnServerClientTraffics" RENAME CONSTRAINT "PK_VpnServerClientTraffics" TO "PK_OpenVpnServerClientTraffics";
                ALTER TABLE xgb_dashopnvpn."VpnServerClients" RENAME CONSTRAINT "PK_VpnServerClients" TO "PK_OpenVpnServerClients";
                """);

            migrationBuilder.RenameIndex(
                name: "IX_VpnServerClients_Server_IsConnected_Session",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClients",
                newName: "IX_OpenVpnServerClients_Server_IsConnected_Session");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServerClients_ConnectedSince_Lat_Lon",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClients",
                newName: "IX_OpenVpnServerClients_ConnectedSince_Lat_Lon");

            migrationBuilder.RenameIndex(
                name: "UX_VpnServerClients_Server_Session",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClients",
                newName: "UX_OpenVpnServerClients_Server_Session");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServerClients_Server_IsConnected",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClients",
                newName: "IX_OpenVpnServerClients_Server_IsConnected");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServerClients_ConnectedSince_ExternalId",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClients",
                newName: "IX_OpenVpnServerClients_ConnectedSince_ExternalId");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServerConflogs_CreateDate",
                schema: "xgb_dashopnvpn",
                table: "VpnServerConflogs",
                newName: "IX_OpenVpnServerConflogs_CreateDate");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServerConflogs_RequestUrl",
                schema: "xgb_dashopnvpn",
                table: "VpnServerConflogs",
                newName: "IX_OpenVpnServerConflogs_RequestUrl");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServerConflogs_VpnServerId",
                schema: "xgb_dashopnvpn",
                table: "VpnServerConflogs",
                newName: "IX_OpenVpnServerConflogs_VpnServerId");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServerTags_TagId",
                schema: "xgb_dashopnvpn",
                table: "VpnServerTags",
                newName: "IX_OpenVpnServerTags_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServerTags_VpnServerId",
                schema: "xgb_dashopnvpn",
                table: "VpnServerTags",
                newName: "IX_OpenVpnServerTags_VpnServerId");

            migrationBuilder.RenameTable(
                name: "VpnServerClients",
                schema: "xgb_dashopnvpn",
                newName: "OpenVpnServerClients",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "VpnServerClientTraffics",
                schema: "xgb_dashopnvpn",
                newName: "OpenVpnServerClientTraffics",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "VpnServerConflogs",
                schema: "xgb_dashopnvpn",
                newName: "OpenVpnServerConflogs",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "VpnServerEventLogs",
                schema: "xgb_dashopnvpn",
                newName: "OpenVpnServerEventLogs",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "VpnServerOvpnFileConfigs",
                schema: "xgb_dashopnvpn",
                newName: "OpenVpnServerOvpnFileConfigs",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "VpnServerStatusLogs",
                schema: "xgb_dashopnvpn",
                newName: "OpenVpnServerStatusLogs",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameTable(
                name: "VpnServerTags",
                schema: "xgb_dashopnvpn",
                newName: "OpenVpnServerTags",
                newSchema: "xgb_dashopnvpn");
        }
    }
}

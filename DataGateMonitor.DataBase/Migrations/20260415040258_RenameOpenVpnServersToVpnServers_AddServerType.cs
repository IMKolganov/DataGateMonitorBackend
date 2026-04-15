using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class RenameOpenVpnServersToVpnServers_AddServerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OpenVpnServers",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers");

            migrationBuilder.RenameTable(
                name: "OpenVpnServers",
                schema: "xgb_dashopnvpn",
                newName: "VpnServers",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServers_ServerName",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                newName: "IX_VpnServers_ServerName");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServers_Latitude_Longitude",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                newName: "IX_VpnServers_Latitude_Longitude");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServers_IsOnline",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                newName: "IX_VpnServers_IsOnline");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServers_IsEnableWss",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                newName: "IX_VpnServers_IsEnableWss");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServers_IsDisable",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                newName: "IX_VpnServers_IsDisable");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServers_IsDeleted",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                newName: "IX_VpnServers_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_OpenVpnServers_IsDefault",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                newName: "IX_VpnServers_IsDefault");

            migrationBuilder.AddColumn<int>(
                name: "ServerType",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_VpnServers",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_VpnServers_ServerType",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                column: "ServerType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_VpnServers",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropIndex(
                name: "IX_VpnServers_ServerType",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropColumn(
                name: "ServerType",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.RenameTable(
                name: "VpnServers",
                schema: "xgb_dashopnvpn",
                newName: "OpenVpnServers",
                newSchema: "xgb_dashopnvpn");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServers_ServerName",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                newName: "IX_OpenVpnServers_ServerName");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServers_Latitude_Longitude",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                newName: "IX_OpenVpnServers_Latitude_Longitude");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServers_IsOnline",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                newName: "IX_OpenVpnServers_IsOnline");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServers_IsEnableWss",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                newName: "IX_OpenVpnServers_IsEnableWss");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServers_IsDisable",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                newName: "IX_OpenVpnServers_IsDisable");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServers_IsDeleted",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                newName: "IX_OpenVpnServers_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_VpnServers_IsDefault",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                newName: "IX_OpenVpnServers_IsDefault");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OpenVpnServers",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServers",
                column: "Id");
        }
    }
}

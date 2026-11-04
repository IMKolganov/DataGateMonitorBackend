using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class LocalIssuance_NodeKeys_IssuedOvpnFileOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Node-local issuance: node event-signing keys + origin/service-profile data on issued files.
            // Seed UpdateData for VpnServers 1–3 omitted (new nullable columns; same as 20260929220021).
            migrationBuilder.AddColumn<string>(
                name: "NodeKeyFingerprint",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodePendingKeyFingerprint",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodePendingPublicKeyPem",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodePublicKeyPem",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodeKeyFingerprint",
                schema: "xgb_dashopnvpn",
                table: "VpnServerDiscoveries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodePublicKeyPem",
                schema: "xgb_dashopnvpn",
                table: "VpnServerDiscoveries",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertFingerprintSha256",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertSerialNumber",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsServiceProfile",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Origin",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StaticIp",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VpnServers_NodeKeyFingerprint",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                column: "NodeKeyFingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedOvpnFiles_VpnServerId_CertSerialNumber",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles",
                columns: new[] { "VpnServerId", "CertSerialNumber" },
                unique: true,
                filter: "\"CertSerialNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VpnServers_NodeKeyFingerprint",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropIndex(
                name: "IX_IssuedOvpnFiles_VpnServerId_CertSerialNumber",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles");

            migrationBuilder.DropColumn(
                name: "NodeKeyFingerprint",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropColumn(
                name: "NodePendingKeyFingerprint",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropColumn(
                name: "NodePendingPublicKeyPem",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropColumn(
                name: "NodePublicKeyPem",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");

            migrationBuilder.DropColumn(
                name: "NodeKeyFingerprint",
                schema: "xgb_dashopnvpn",
                table: "VpnServerDiscoveries");

            migrationBuilder.DropColumn(
                name: "NodePublicKeyPem",
                schema: "xgb_dashopnvpn",
                table: "VpnServerDiscoveries");

            migrationBuilder.DropColumn(
                name: "CertFingerprintSha256",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles");

            migrationBuilder.DropColumn(
                name: "CertSerialNumber",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles");

            migrationBuilder.DropColumn(
                name: "IsServiceProfile",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles");

            migrationBuilder.DropColumn(
                name: "Origin",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles");

            migrationBuilder.DropColumn(
                name: "StaticIp",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles");
        }
    }
}

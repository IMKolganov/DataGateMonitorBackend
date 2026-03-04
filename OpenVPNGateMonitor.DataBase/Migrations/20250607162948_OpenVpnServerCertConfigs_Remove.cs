using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerCertConfigs_Remove : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenVpnServerCertConfigs",
                schema: "xgb_dashopnvpn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenVpnServerCertConfigs",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaCertPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CrlOpenvpnPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CrlPkiPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EasyRsaPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    OvpnFileDir = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PkiPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RevokedOvpnFilesDirPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ServerRemoteIp = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StatusFilePath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TlsAuthKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    VpnServerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenVpnServerCertConfigs", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerCertConfigs",
                columns: new[] { "Id", "CaCertPath", "CrlOpenvpnPath", "CrlPkiPath", "EasyRsaPath", "OvpnFileDir", "PkiPath", "RevokedOvpnFilesDirPath", "ServerRemoteIp", "StatusFilePath", "TlsAuthKey", "VpnServerId" },
                values: new object[,]
                {
                    { 1, "/openvpn-udp/easy-rsa/pki/ca.crt", "/openvpn-udp/crl.pem", "/openvpn-udp/easy-rsa/pki/crl.pem", "/openvpn-udp/easy-rsa", "/openvpn-udp/clients", "/openvpn-udp/easy-rsa/pki/", "/openvpn-udp/clients/revoked/", "0.0.0.0", "/var/log/openvpn-status.log", "/openvpn-udp/easy-rsa/pki/ta.key", 1 },
                    { 2, "/openvpn-tcp/easy-rsa/pki/ca.crt", "/openvpn-tcp/crl.pem", "/openvpn-tcp/easy-rsa/pki/crl.pem", "/openvpn-tcp/easy-rsa", "/openvpn-tcp/clients", "/openvpn-tcp/easy-rsa/pki/", "/openvpn-tcp/clients/revoked/", "0.0.0.0", "/var/log/openvpn-status.log", "/openvpn-tcp/easy-rsa/pki/ta.key", 2 }
                });
        }
    }
}
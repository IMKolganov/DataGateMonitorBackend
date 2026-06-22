using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class VpnDnsQueryLog_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VpnDnsQueryLogs",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VpnServerId = table.Column<int>(type: "integer", nullable: false),
                    PiHoleQueryId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CommonName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ClientIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Domain = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    QueryType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    QueriedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VpnDnsQueryLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vpn_dns_query_server_domain_time",
                schema: "xgb_dashopnvpn",
                table: "VpnDnsQueryLogs",
                columns: new[] { "VpnServerId", "Domain", "QueriedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "ix_vpn_dns_query_server_external_time",
                schema: "xgb_dashopnvpn",
                table: "VpnDnsQueryLogs",
                columns: new[] { "VpnServerId", "ExternalId", "QueriedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "ix_vpn_dns_query_server_time",
                schema: "xgb_dashopnvpn",
                table: "VpnDnsQueryLogs",
                columns: new[] { "VpnServerId", "QueriedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "ux_vpn_dns_query_server_pihole_id",
                schema: "xgb_dashopnvpn",
                table: "VpnDnsQueryLogs",
                columns: new[] { "VpnServerId", "PiHoleQueryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VpnDnsQueryLogs",
                schema: "xgb_dashopnvpn");
        }
    }
}

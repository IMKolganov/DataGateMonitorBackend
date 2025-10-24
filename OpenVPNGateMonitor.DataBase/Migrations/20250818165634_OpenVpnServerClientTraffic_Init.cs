using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerClientTraffic_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenVpnServerClientTraffics",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VpnServerId = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BytesReceived = table.Column<long>(type: "bigint", nullable: false),
                    BytesSent = table.Column<long>(type: "bigint", nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenVpnServerClientTraffics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTraffic_External_At",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics",
                columns: new[] { "ExternalId", "MeasuredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTraffic_Server_At",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics",
                columns: new[] { "VpnServerId", "MeasuredAt" });

            migrationBuilder.CreateIndex(
                name: "UX_ClientTraffic_Server_Session_At",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics",
                columns: new[] { "VpnServerId", "SessionId", "MeasuredAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenVpnServerClientTraffics",
                schema: "xgb_dashopnvpn");
        }
    }
}
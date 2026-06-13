using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class VpnServerClientTrafficDaily_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VpnServerClientTrafficDailies",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VpnServerId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    TrafficInBytes = table.Column<long>(type: "bigint", nullable: false),
                    TrafficOutBytes = table.Column<long>(type: "bigint", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VpnServerClientTrafficDailies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTrafficDaily_Day_External",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClientTrafficDailies",
                columns: new[] { "DayUtc", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTrafficDaily_Day_Server",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClientTrafficDailies",
                columns: new[] { "DayUtc", "VpnServerId" });

            migrationBuilder.CreateIndex(
                name: "UX_ClientTrafficDaily_Server_Session_Day",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClientTrafficDailies",
                columns: new[] { "VpnServerId", "SessionId", "DayUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VpnServerClientTrafficDailies",
                schema: "xgb_dashopnvpn");
        }
    }
}

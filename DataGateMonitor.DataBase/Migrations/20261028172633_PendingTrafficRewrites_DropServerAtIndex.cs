using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class PendingTrafficRewrites_DropServerAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prod already dropped this with DROP INDEX CONCURRENTLY. A plain DropIndex fails there.
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS xgb_dashopnvpn."IX_ClientTraffic_Server_At";
                """);

            migrationBuilder.CreateTable(
                name: "PendingTrafficRewrites",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OldExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NewExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SurvivorUserId = table.Column<int>(type: "integer", nullable: false),
                    MergedUserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NextRangeEndExclusive = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastProcessedChunkName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RowsUpdated = table.Column<long>(type: "bigint", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingTrafficRewrites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingTrafficRewrites_Status",
                schema: "xgb_dashopnvpn",
                table: "PendingTrafficRewrites",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PendingTrafficRewrites_Status_Id",
                schema: "xgb_dashopnvpn",
                table: "PendingTrafficRewrites",
                columns: new[] { "Status", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingTrafficRewrites",
                schema: "xgb_dashopnvpn");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTraffic_Server_At",
                schema: "xgb_dashopnvpn",
                table: "VpnServerClientTraffics",
                columns: new[] { "VpnServerId", "MeasuredAt" });
        }
    }
}

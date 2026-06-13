using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class MobileCrashReports_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MobileCrashReports",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppProcess = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PayloadRaw = table.Column<string>(type: "text", nullable: false),
                    ParseStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Process = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Thread = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Sdk = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Device = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Exception = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Tag = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Stacktrace = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileCrashReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MobileCrashReports_AppProcess_CreateDate",
                schema: "xgb_dashopnvpn",
                table: "MobileCrashReports",
                columns: new[] { "AppProcess", "CreateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MobileCrashReports_CreateDate",
                schema: "xgb_dashopnvpn",
                table: "MobileCrashReports",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_MobileCrashReports_ParseStatus",
                schema: "xgb_dashopnvpn",
                table: "MobileCrashReports",
                column: "ParseStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MobileCrashReports",
                schema: "xgb_dashopnvpn");
        }
    }
}

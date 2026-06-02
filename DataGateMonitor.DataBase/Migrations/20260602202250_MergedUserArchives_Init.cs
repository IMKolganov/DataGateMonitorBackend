using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class MergedUserArchives_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MergedUserArchives",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OriginalUserId = table.Column<int>(type: "integer", nullable: false),
                    MergedIntoUserId = table.Column<int>(type: "integer", nullable: false),
                    MergedByUserId = table.Column<int>(type: "integer", nullable: true),
                    MergedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsEmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    HasDashboardAccess = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalCreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OriginalLastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdentityLinksJson = table.Column<string>(type: "jsonb", nullable: false),
                    MergeReportJson = table.Column<string>(type: "jsonb", nullable: false),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MergedUserArchives", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                columns: new[] { "Id", "Enabled", "Kind" },
                values: new object[,]
                {
                    { 30, true, 29 },
                    { 31, true, 30 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MergedUserArchives_MergedAt",
                schema: "xgb_dashopnvpn",
                table: "MergedUserArchives",
                column: "MergedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MergedUserArchives_MergedIntoUserId",
                schema: "xgb_dashopnvpn",
                table: "MergedUserArchives",
                column: "MergedIntoUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MergedUserArchives_OriginalUserId",
                schema: "xgb_dashopnvpn",
                table: "MergedUserArchives",
                column: "OriginalUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MergedUserArchives",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                keyColumn: "Id",
                keyValue: 31);
        }
    }
}

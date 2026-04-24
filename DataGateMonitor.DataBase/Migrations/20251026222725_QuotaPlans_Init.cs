using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class QuotaPlans_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuotaPlanAllowedServers",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    QuotaPlanId = table.Column<int>(type: "integer", nullable: false),
                    VpnServerId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotaPlanAllowedServers", x => new { x.QuotaPlanId, x.VpnServerId });
                });

            migrationBuilder.CreateTable(
                name: "QuotaPlans",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DailyQuotaBytes = table.Column<long>(type: "bigint", nullable: true),
                    MonthlyQuotaBytes = table.Column<long>(type: "bigint", nullable: true),
                    UpKbps = table.Column<int>(type: "integer", nullable: true),
                    DownKbps = table.Column<int>(type: "integer", nullable: true),
                    OverlimitAction = table.Column<int>(type: "integer", nullable: false),
                    ThrottleUpKbps = table.Column<int>(type: "integer", nullable: true),
                    ThrottleDownKbps = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotaPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserQuotaPlans",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    QuotaPlanId = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignedBy = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuotaPlans", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "QuotaPlans",
                columns: new[] { "Id", "DailyQuotaBytes", "Description", "DownKbps", "IsActive", "MonthlyQuotaBytes", "Name", "OverlimitAction", "ThrottleDownKbps", "ThrottleUpKbps", "UpKbps" },
                values: new object[] { 1, 5368709120L, "Entry plan (5 GB/day, 20 GB/month)", 2048, true, 21474836480L, "Free", 1, 256, 128, 1024 });

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "QuotaPlans",
                columns: new[] { "Id", "DailyQuotaBytes", "Description", "DownKbps", "IsActive", "IsDefault", "MonthlyQuotaBytes", "Name", "OverlimitAction", "ThrottleDownKbps", "ThrottleUpKbps", "UpKbps" },
                values: new object[] { 2, 10737418240L, "Default plan (10 GB/day, 50 GB/month)", 4096, true, true, 53687091200L, "Default", 1, 512, 256, 2048 });

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "QuotaPlans",
                columns: new[] { "Id", "DailyQuotaBytes", "Description", "DownKbps", "IsActive", "MonthlyQuotaBytes", "Name", "OverlimitAction", "ThrottleDownKbps", "ThrottleUpKbps", "UpKbps" },
                values: new object[,]
                {
                    { 3, 21474836480L, "Balanced plan (20 GB/day, 100 GB/month)", 8192, true, 107374182400L, "Standard", 2, null, null, 4096 },
                    { 4, 53687091200L, "Heavy users (50 GB/day, 300 GB/month)", 16384, true, 322122547200L, "Pro", 3, null, null, 8192 },
                    { 5, null, "No traffic limits", null, true, null, "Unlimited", 0, null, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuotaPlanAllowedServers_QuotaPlanId",
                schema: "xgb_dashopnvpn",
                table: "QuotaPlanAllowedServers",
                column: "QuotaPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotaPlans_IsDefault",
                schema: "xgb_dashopnvpn",
                table: "QuotaPlans",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_QuotaPlans_Name",
                schema: "xgb_dashopnvpn",
                table: "QuotaPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserQuotaPlans_EffectiveFrom",
                schema: "xgb_dashopnvpn",
                table: "UserQuotaPlans",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuotaPlans_UserId",
                schema: "xgb_dashopnvpn",
                table: "UserQuotaPlans",
                column: "UserId",
                unique: true,
                filter: "\"EffectiveTo\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuotaPlanAllowedServers",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DropTable(
                name: "QuotaPlans",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DropTable(
                name: "UserQuotaPlans",
                schema: "xgb_dashopnvpn");
        }
    }
}

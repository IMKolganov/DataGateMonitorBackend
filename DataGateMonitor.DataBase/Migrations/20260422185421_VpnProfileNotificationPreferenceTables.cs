using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class VpnProfileNotificationPreferenceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            for (var id = 26; id >= 15; id--)
            {
                migrationBuilder.DeleteData(
                    schema: "xgb_dashopnvpn",
                    table: "Settings",
                    keyColumn: "Id",
                    keyValue: id);
            }

            migrationBuilder.CreateTable(
                name: "VpnProfileNotificationGlobalPreferences",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GloballyEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VpnProfileNotificationGlobalPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VpnProfileNotificationPreferences",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Stack = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VpnProfileNotificationPreferences", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationGlobalPreferences",
                columns: new[] { "Id", "GloballyEnabled" },
                values: new object[] { 1, true });

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                columns: new[] { "Id", "Category", "Enabled", "Stack" },
                values: new object[,]
                {
                    { 1, 0, true, 0 },
                    { 2, 1, true, 0 },
                    { 3, 2, false, 0 },
                    { 4, 0, true, 1 },
                    { 5, 1, true, 1 },
                    { 6, 2, false, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VpnProfileNotificationPreferences_Stack_Category",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                columns: new[] { "Stack", "Category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VpnProfileNotificationGlobalPreferences",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DropTable(
                name: "VpnProfileNotificationPreferences",
                schema: "xgb_dashopnvpn");
        }
    }
}

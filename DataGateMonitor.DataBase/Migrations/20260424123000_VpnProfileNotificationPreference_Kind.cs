using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class VpnProfileNotificationPreference_Kind : Migration
    {
        private static readonly DateTimeOffset SeedEpoch = new(1, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VpnProfileNotificationPreferences_Stack_Category",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "xgb_dashopnvpn"."VpnProfileNotificationPreferences"
                SET "Kind" = "Stack" * 3 + "Category";
                """);

            migrationBuilder.DropColumn(
                name: "Stack",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences");

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                columns: new[] { "Id", "Kind", "Enabled", "CreateDate", "LastUpdate" },
                values: new object[,]
                {
                    { 7, 6, true, SeedEpoch, SeedEpoch },
                    { 8, 7, true, SeedEpoch, SeedEpoch },
                    { 9, 8, true, SeedEpoch, SeedEpoch },
                    { 10, 9, true, SeedEpoch, SeedEpoch },
                    { 11, 10, true, SeedEpoch, SeedEpoch },
                    { 12, 11, true, SeedEpoch, SeedEpoch },
                    { 13, 12, true, SeedEpoch, SeedEpoch },
                    { 14, 13, true, SeedEpoch, SeedEpoch },
                    { 15, 14, true, SeedEpoch, SeedEpoch },
                    { 16, 15, true, SeedEpoch, SeedEpoch },
                    { 17, 16, true, SeedEpoch, SeedEpoch },
                    { 18, 17, true, SeedEpoch, SeedEpoch },
                    { 19, 18, true, SeedEpoch, SeedEpoch },
                    { 20, 19, true, SeedEpoch, SeedEpoch },
                    { 21, 20, true, SeedEpoch, SeedEpoch },
                    { 22, 21, true, SeedEpoch, SeedEpoch },
                    { 23, 22, true, SeedEpoch, SeedEpoch },
                    { 24, 23, true, SeedEpoch, SeedEpoch },
                    { 25, 24, true, SeedEpoch, SeedEpoch },
                    { 26, 25, true, SeedEpoch, SeedEpoch },
                    { 27, 26, true, SeedEpoch, SeedEpoch }
                });

            migrationBuilder.AlterColumn<int>(
                name: "Kind",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VpnProfileNotificationPreferences_Kind",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                column: "Kind",
                unique: true);

            migrationBuilder.Sql(
                """
                SELECT setval(
                    pg_get_serial_sequence('"xgb_dashopnvpn"."VpnProfileNotificationPreferences"', 'Id'),
                    (SELECT COALESCE(MAX("Id"), 1) FROM "xgb_dashopnvpn"."VpnProfileNotificationPreferences"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VpnProfileNotificationPreferences_Kind",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences");

            migrationBuilder.AddColumn<int>(
                name: "Stack",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "xgb_dashopnvpn"."VpnProfileNotificationPreferences"
                SET "Stack" = "Kind" / 3,
                    "Category" = "Kind" % 3
                WHERE "Id" <= 6;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "xgb_dashopnvpn"."VpnProfileNotificationPreferences"
                WHERE "Id" > 6;
                """);

            migrationBuilder.DropColumn(
                name: "Kind",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences");

            migrationBuilder.AlterColumn<int>(
                name: "Stack",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VpnProfileNotificationPreferences_Stack_Category",
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                columns: new[] { "Stack", "Category" },
                unique: true);

            migrationBuilder.Sql(
                """
                SELECT setval(
                    pg_get_serial_sequence('"xgb_dashopnvpn"."VpnProfileNotificationPreferences"', 'Id'),
                    (SELECT COALESCE(MAX("Id"), 1) FROM "xgb_dashopnvpn"."VpnProfileNotificationPreferences"));
                """);
        }
    }
}

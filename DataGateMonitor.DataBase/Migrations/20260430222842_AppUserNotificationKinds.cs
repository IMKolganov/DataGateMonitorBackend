using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class AppUserNotificationKinds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                columns: new[] { "Id", "Enabled", "Kind" },
                values: new object[,]
                {
                    { 28, true, 27 },
                    { 29, true, 28 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                keyColumn: "Id",
                keyValue: 29);
        }
    }
}

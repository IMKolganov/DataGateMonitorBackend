using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class GeoIp_Auto_Update_Interval_Days : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                columns: new[] { "Id", "BoolValue", "DateTimeValue", "DoubleValue", "IntValue", "Key", "StringValue", "ValueType" },
                values: new object[,]
                {
                    { 13, null, null, null, 0, "GeoIp_Auto_Update_Interval_Days", null, "int" },
                    { 14, null, null, null, null, "GeoIp_Auto_Update_Interval_Days_Type", "int", "string" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                keyColumn: "Id",
                keyValue: 14);
        }
    }
}

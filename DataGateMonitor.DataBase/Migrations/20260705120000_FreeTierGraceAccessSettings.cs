using DataGateMonitor.DataBase.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Designer-less data migration: columnTypes required so EF can generate SQL
    /// without a BuildTargetModel that maps Settings.
    /// </remarks>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260705120000_FreeTierGraceAccessSettings")]
    public partial class FreeTierGraceAccessSettings : Migration
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
                    { 19, false, null, null, null, "FreeTier_Allow_Grace_Without_Compliance", null, "bool" },
                    { 20, null, null, null, null, "FreeTier_Allow_Grace_Without_Compliance_Type", "bool", "string" },
                    { 21, null, null, null, 5, "FreeTier_Grace_Period_Minutes", null, "int" },
                    { 22, null, null, null, null, "FreeTier_Grace_Period_Minutes_Type", "int", "string" }
                },
                columnTypes: new[]
                {
                    "integer",
                    "boolean",
                    "timestamp with time zone",
                    "double precision",
                    "integer",
                    "character varying(255)",
                    "character varying(255)",
                    "character varying(50)"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                keyColumn: "Id",
                keyColumnType: "integer",
                keyValue: 19);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                keyColumn: "Id",
                keyColumnType: "integer",
                keyValue: 20);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                keyColumn: "Id",
                keyColumnType: "integer",
                keyValue: 21);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                keyColumn: "Id",
                keyColumnType: "integer",
                keyValue: 22);
        }
    }
}

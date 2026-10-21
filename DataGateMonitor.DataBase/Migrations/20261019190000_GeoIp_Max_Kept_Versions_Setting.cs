using DataGateMonitor.DataBase.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261019190000_GeoIp_Max_Kept_Versions_Setting")]
    public partial class GeoIp_Max_Kept_Versions_Setting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent by Key. Do NOT hardcode Id — Settings uses identity; fixed Ids
            // collide with rows already allocated by the sequence (23505 PK_Settings).
            migrationBuilder.Sql(
                """
                INSERT INTO xgb_dashopnvpn."Settings"
                    ("BoolValue", "DateTimeValue", "DoubleValue", "IntValue", "Key", "StringValue", "ValueType", "CreateDate", "LastUpdate")
                SELECT NULL, NULL, NULL, 10, 'GeoIp_Max_Kept_Versions', NULL, 'int', TIMESTAMPTZ '-infinity', TIMESTAMPTZ '-infinity'
                WHERE NOT EXISTS (
                    SELECT 1 FROM xgb_dashopnvpn."Settings" WHERE "Key" = 'GeoIp_Max_Kept_Versions'
                );

                INSERT INTO xgb_dashopnvpn."Settings"
                    ("BoolValue", "DateTimeValue", "DoubleValue", "IntValue", "Key", "StringValue", "ValueType", "CreateDate", "LastUpdate")
                SELECT NULL, NULL, NULL, NULL, 'GeoIp_Max_Kept_Versions_Type', 'int', 'string', TIMESTAMPTZ '-infinity', TIMESTAMPTZ '-infinity'
                WHERE NOT EXISTS (
                    SELECT 1 FROM xgb_dashopnvpn."Settings" WHERE "Key" = 'GeoIp_Max_Kept_Versions_Type'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM xgb_dashopnvpn."Settings"
                WHERE "Key" IN ('GeoIp_Max_Kept_Versions', 'GeoIp_Max_Kept_Versions_Type');
                """);
        }
    }
}

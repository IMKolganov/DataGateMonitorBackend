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
            // Idempotent: safe if keys already exist (manual insert / partial apply).
            migrationBuilder.Sql(
                """
                INSERT INTO xgb_dashopnvpn."Settings"
                    ("Id", "BoolValue", "DateTimeValue", "DoubleValue", "IntValue", "Key", "StringValue", "ValueType", "CreateDate", "LastUpdate")
                SELECT 212, NULL, NULL, NULL, 10, 'GeoIp_Max_Kept_Versions', NULL, 'int', TIMESTAMPTZ '-infinity', TIMESTAMPTZ '-infinity'
                WHERE NOT EXISTS (
                    SELECT 1 FROM xgb_dashopnvpn."Settings" WHERE "Key" = 'GeoIp_Max_Kept_Versions'
                );

                INSERT INTO xgb_dashopnvpn."Settings"
                    ("Id", "BoolValue", "DateTimeValue", "DoubleValue", "IntValue", "Key", "StringValue", "ValueType", "CreateDate", "LastUpdate")
                SELECT 213, NULL, NULL, NULL, NULL, 'GeoIp_Max_Kept_Versions_Type', 'int', 'string', TIMESTAMPTZ '-infinity', TIMESTAMPTZ '-infinity'
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

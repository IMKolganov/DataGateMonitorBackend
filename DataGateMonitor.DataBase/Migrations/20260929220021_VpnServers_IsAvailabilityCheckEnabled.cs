using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class VpnServers_IsAvailabilityCheckEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep only the column add. Seed UpdateData for Ids 1–3 is redundant
            // (defaultValue: true already applies to existing rows) and would conflict
            // if those seed rows were renamed/deleted in prod.
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailabilityCheckEnabled",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailabilityCheckEnabled",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");
        }
    }
}

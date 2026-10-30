using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class VpnServerDiscovery_PiHoleBootstrap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasPiHole",
                schema: "xgb_dashopnvpn",
                table: "VpnServerDiscoveries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PiHoleConfigJson",
                schema: "xgb_dashopnvpn",
                table: "VpnServerDiscoveries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasPiHole",
                schema: "xgb_dashopnvpn",
                table: "VpnServerDiscoveries");

            migrationBuilder.DropColumn(
                name: "PiHoleConfigJson",
                schema: "xgb_dashopnvpn",
                table: "VpnServerDiscoveries");
        }
    }
}

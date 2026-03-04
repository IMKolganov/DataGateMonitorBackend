using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerClient_UserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClientTraffics");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients");
        }
    }
}

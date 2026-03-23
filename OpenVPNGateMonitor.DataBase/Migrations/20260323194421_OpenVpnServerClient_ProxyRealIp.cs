using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerClient_ProxyRealIp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProxyRealIp",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProxyRealIp",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients");
        }
    }
}

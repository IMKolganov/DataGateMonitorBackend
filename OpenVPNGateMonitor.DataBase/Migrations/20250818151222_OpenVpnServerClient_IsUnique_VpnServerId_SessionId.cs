using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerClient_IsUnique_VpnServerId_SessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_OpenVpnServerClients_Server_Session",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients",
                columns: new[] { "VpnServerId", "SessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_OpenVpnServerClients_Server_Session",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerClient_ExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerClients");
        }
    }
}
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class VpnServerOvpnFileConfigSeedXrayServer3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "VpnServerOvpnFileConfigs",
                columns: new[] { "Id", "ConfigTemplate", "VpnServerId", "VpnServerIp", "VpnServerPort" },
                values: new object[] { 3, "{{vless_uri}}\n# {{friendly_name}}\nUUID: {{uuid}}\nEndpoint: {{server_ip}}:{{server_port}}\n", 3, "127.0.0.1", 443 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "VpnServerOvpnFileConfigs",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}

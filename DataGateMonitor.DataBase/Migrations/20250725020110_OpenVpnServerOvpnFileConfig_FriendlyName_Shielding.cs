using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerOvpnFileConfig_FriendlyName_Shielding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerOvpnFileConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConfigTemplate",
                value: "setenv FRIENDLY_NAME \"{{friendly_name}}\"\nclient\ndev tun\nproto udp\nremote {{server_ip}} {{server_port}}\nresolv-retry infinite\nnobind\nremote-cert-tls server\ntls-version-min 1.2\ncipher AES-256-CBC\nauth SHA256\nauth-nocache\nverb 3\n<ca>\n{{ca_cert}}\n</ca>\n<cert>\n{{client_cert}}\n</cert>\n<key>\n{{client_key}}\n</key>\n<tls-crypt>\n{{tls_auth_key}}\n</tls-crypt>");

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerOvpnFileConfigs",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConfigTemplate",
                value: "setenv FRIENDLY_NAME \"{{friendly_name}}\"\nclient\ndev tun\nproto tcp\nremote {{server_ip}} {{server_port}}\nresolv-retry infinite\nnobind\nremote-cert-tls server\ntls-version-min 1.2\ncipher AES-256-CBC\nauth SHA256\nauth-nocache\nverb 3\n<ca>\n{{ca_cert}}\n</ca>\n<cert>\n{{client_cert}}\n</cert>\n<key>\n{{client_key}}\n</key>\n<tls-crypt>\n{{tls_auth_key}}\n</tls-crypt>");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerOvpnFileConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConfigTemplate",
                value: "setenv FRIENDLY_NAME {{friendly_name}}\nclient\ndev tun\nproto udp\nremote {{server_ip}} {{server_port}}\nresolv-retry infinite\nnobind\nremote-cert-tls server\ntls-version-min 1.2\ncipher AES-256-CBC\nauth SHA256\nauth-nocache\nverb 3\n<ca>\n{{ca_cert}}\n</ca>\n<cert>\n{{client_cert}}\n</cert>\n<key>\n{{client_key}}\n</key>\n<tls-crypt>\n{{tls_auth_key}}\n</tls-crypt>");

            migrationBuilder.UpdateData(
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerOvpnFileConfigs",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConfigTemplate",
                value: "client\ndev tun\nproto tcp\nremote {{server_ip}} {{server_port}}\nresolv-retry infinite\nnobind\nremote-cert-tls server\ntls-version-min 1.2\ncipher AES-256-CBC\nauth SHA256\nauth-nocache\nverb 3\n<ca>\n{{ca_cert}}\n</ca>\n<cert>\n{{client_cert}}\n</cert>\n<key>\n{{client_key}}\n</key>\n<tls-crypt>\n{{tls_auth_key}}\n</tls-crypt>");
        }
    }
}
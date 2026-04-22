using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class NotificationVpnProfileSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                columns: new[] { "Id", "BoolValue", "DateTimeValue", "DoubleValue", "IntValue", "Key", "StringValue", "ValueType" },
                values: new object[,]
                {
                    { 15, true, null, null, null, "Notifications_Vpn_OpenVpn_Read", null, "bool" },
                    { 16, null, null, null, null, "Notifications_Vpn_OpenVpn_Read_Type", "bool", "string" },
                    { 17, true, null, null, null, "Notifications_Vpn_OpenVpn_Mutate", null, "bool" },
                    { 18, null, null, null, null, "Notifications_Vpn_OpenVpn_Mutate_Type", "bool", "string" },
                    { 19, false, null, null, null, "Notifications_Vpn_OpenVpn_Download", null, "bool" },
                    { 20, null, null, null, null, "Notifications_Vpn_OpenVpn_Download_Type", "bool", "string" },
                    { 21, true, null, null, null, "Notifications_Vpn_Xray_Read", null, "bool" },
                    { 22, null, null, null, null, "Notifications_Vpn_Xray_Read_Type", "bool", "string" },
                    { 23, true, null, null, null, "Notifications_Vpn_Xray_Mutate", null, "bool" },
                    { 24, null, null, null, null, "Notifications_Vpn_Xray_Mutate_Type", "bool", "string" },
                    { 25, false, null, null, null, "Notifications_Vpn_Xray_Download", null, "bool" },
                    { 26, null, null, null, null, "Notifications_Vpn_Xray_Download_Type", "bool", "string" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (var id = 26; id >= 15; id--)
            {
                migrationBuilder.DeleteData(
                    schema: "xgb_dashopnvpn",
                    table: "Settings",
                    keyColumn: "Id",
                    keyValue: id);
            }
        }
    }
}

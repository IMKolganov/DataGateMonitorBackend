using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class AuthRequireEmailConfirmationSetting : Migration
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
                    { 15, true, null, null, null, "Auth_Require_Email_Confirmation_On_Register", null, "bool" },
                    { 16, null, null, null, null, "Auth_Require_Email_Confirmation_On_Register_Type", "bool", "string" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                keyColumn: "Id",
                keyValue: 16);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class UserCredential_AdminTotp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TotpEnabledAt",
                schema: "xgb_dashopnvpn",
                table: "UserCredentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TotpSecretEncrypted",
                schema: "xgb_dashopnvpn",
                table: "UserCredentials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotpEnabledAt",
                schema: "xgb_dashopnvpn",
                table: "UserCredentials");

            migrationBuilder.DropColumn(
                name: "TotpSecretEncrypted",
                schema: "xgb_dashopnvpn",
                table: "UserCredentials");
        }
    }
}

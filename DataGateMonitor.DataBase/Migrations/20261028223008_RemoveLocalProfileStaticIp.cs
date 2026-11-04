using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLocalProfileStaticIp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StaticIp",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StaticIp",
                schema: "xgb_dashopnvpn",
                table: "IssuedOvpnFiles",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class VpnServerPiHole_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPiHoleEnabled",
                schema: "xgb_dashopnvpn",
                table: "VpnServers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "VpnServerPiHoleConfigs",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VpnServerId = table.Column<int>(type: "integer", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AppPassword = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PollIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    BatchSize = table.Column<int>(type: "integer", nullable: false),
                    LookbackSeconds = table.Column<int>(type: "integer", nullable: false),
                    ClientSubnetPrefix = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VpnServerPiHoleConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_vpn_server_pihole_config_server_id",
                schema: "xgb_dashopnvpn",
                table: "VpnServerPiHoleConfigs",
                column: "VpnServerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VpnServerPiHoleConfigs",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DropColumn(
                name: "IsPiHoleEnabled",
                schema: "xgb_dashopnvpn",
                table: "VpnServers");
        }
    }
}

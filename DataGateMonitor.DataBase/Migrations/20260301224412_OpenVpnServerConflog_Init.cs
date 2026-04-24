using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerConflog_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenVpnServerConflogs",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VpnServerId = table.Column<int>(type: "integer", nullable: true),
                    RequestUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenVpnServerConflogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServerConflogs_CreateDate",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerConflogs",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServerConflogs_RequestUrl",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerConflogs",
                column: "RequestUrl");

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServerConflogs_VpnServerId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerConflogs",
                column: "VpnServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenVpnServerConflogs",
                schema: "xgb_dashopnvpn");
        }
    }
}

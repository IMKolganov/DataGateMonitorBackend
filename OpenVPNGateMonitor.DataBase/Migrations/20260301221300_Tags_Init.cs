using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class Tags_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenVpnServerTags",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    VpnServerId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenVpnServerTags", x => new { x.TagId, x.VpnServerId });
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServerTags_TagId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenVpnServerTags_VpnServerId",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerTags",
                column: "VpnServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                schema: "xgb_dashopnvpn",
                table: "Tags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenVpnServerTags",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DropTable(
                name: "Tags",
                schema: "xgb_dashopnvpn");
        }
    }
}

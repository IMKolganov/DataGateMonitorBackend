using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class IssuedXrayClientLinks_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssuedXrayClientLinks",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VpnServerId = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CommonName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CertId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IssuedTo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PemFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CertFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    KeyFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReqFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedXrayClientLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssuedXrayClientLinkTokens",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IssuedXrayClientLinkId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedXrayClientLinkTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssuedXrayClientLinkTokens_IssuedXrayClientLinks_IssuedXray~",
                        column: x => x.IssuedXrayClientLinkId,
                        principalSchema: "xgb_dashopnvpn",
                        principalTable: "IssuedXrayClientLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssuedXrayClientLinkTokens_IssuedXrayClientLinkId",
                schema: "xgb_dashopnvpn",
                table: "IssuedXrayClientLinkTokens",
                column: "IssuedXrayClientLinkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssuedXrayClientLinkTokens",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DropTable(
                name: "IssuedXrayClientLinks",
                schema: "xgb_dashopnvpn");
        }
    }
}

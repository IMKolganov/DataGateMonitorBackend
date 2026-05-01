using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class Users_AvatarUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                schema: "xgb_dashopnvpn",
                table: "Users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TelegramBotUserProfilePhotos",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramBotUserId = table.Column<int>(type: "integer", nullable: false),
                    ImageBytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TelegramFileUniqueId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramBotUserProfilePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramBotUserProfilePhotos_TelegramBotUsers_TelegramBotUs~",
                        column: x => x.TelegramBotUserId,
                        principalSchema: "xgb_dashopnvpn",
                        principalTable: "TelegramBotUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBotUserProfilePhotos_TelegramBotUserId",
                schema: "xgb_dashopnvpn",
                table: "TelegramBotUserProfilePhotos",
                column: "TelegramBotUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramBotUserProfilePhotos",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                schema: "xgb_dashopnvpn",
                table: "Users");
        }
    }
}

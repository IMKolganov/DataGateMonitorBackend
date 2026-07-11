using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class UserPasswordHistory_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPasswordHistory",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    UserCredentialId = table.Column<int>(type: "integer", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PasswordAlgo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SetByActor = table.Column<int>(type: "integer", nullable: false),
                    SetByUserId = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPasswordHistory", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                columns: new[] { "Id", "Enabled", "Kind" },
                values: new object[,]
                {
                    { 32, true, 31 },
                    { 33, true, 32 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPasswordHistory_UserId",
                schema: "xgb_dashopnvpn",
                table: "UserPasswordHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPasswordHistory_UserId_RecordedAtUtc",
                schema: "xgb_dashopnvpn",
                table: "UserPasswordHistory",
                columns: new[] { "UserId", "RecordedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPasswordHistory",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "VpnProfileNotificationPreferences",
                keyColumn: "Id",
                keyValue: 33);
        }
    }
}

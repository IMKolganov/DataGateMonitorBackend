using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class FreeTierDisconnectLog_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FreeTierDisconnectLogs",
                schema: "xgb_dashopnvpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    UserDisplayNameSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    VpnServerId = table.Column<int>(type: "integer", nullable: false),
                    VpnServerNameSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CommonName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ManagementClientId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    InitiatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RevokeRequested = table.Column<bool>(type: "boolean", nullable: false),
                    RevokeSucceeded = table.Column<bool>(type: "boolean", nullable: true),
                    KillSucceeded = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreeTierDisconnectLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FreeTierDisconnectLogs_CreatedAt",
                schema: "xgb_dashopnvpn",
                table: "FreeTierDisconnectLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FreeTierDisconnectLogs_UserId",
                schema: "xgb_dashopnvpn",
                table: "FreeTierDisconnectLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FreeTierDisconnectLogs_VpnServerId",
                schema: "xgb_dashopnvpn",
                table: "FreeTierDisconnectLogs",
                column: "VpnServerId");

            // Ids 200/201 are used instead of the next sequential number because a few earlier
            // Settings-seed migrations (e.g. FreeTierGraceAccessSettings, FreeTierOpenVpnEnforcementSettings)
            // were committed without a matching .Designer.cs and are not actually applied by `dotnet ef
            // database update` on a fresh database, leaving their intended ids (19-26) unreliable. Starting
            // at 200 avoids colliding with any of that pre-existing range once it gets fixed.
            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                columns: new[] { "Id", "BoolValue", "DateTimeValue", "DoubleValue", "IntValue", "Key", "StringValue", "ValueType" },
                values: new object[,]
                {
                    { 200, false, null, null, null, "FreeTier_Revoke_Ovpn_On_Enforcement", null, "bool" },
                    { 201, null, null, null, null, "FreeTier_Revoke_Ovpn_On_Enforcement_Type", "bool", "string" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FreeTierDisconnectLogs",
                schema: "xgb_dashopnvpn");

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                keyColumn: "Id",
                keyValue: 200);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "Settings",
                keyColumn: "Id",
                keyValue: 201);
        }
    }
}

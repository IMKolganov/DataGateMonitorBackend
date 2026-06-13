using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class LocalIdentityLinks_Backfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO xgb_dashopnvpn."UserIdentityLinks" ("UserId", "Provider", "ExternalId", "CreateDate", "LastUpdate")
                SELECT c."UserId", 'local', 'local:' || c."UserId"::text, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM xgb_dashopnvpn."UserCredentials" c
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM xgb_dashopnvpn."UserIdentityLinks" l
                    WHERE l."UserId" = c."UserId"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM xgb_dashopnvpn."UserIdentityLinks" l
                WHERE l."Provider" = 'local'
                  AND l."ExternalId" = 'local:' || l."UserId"::text
                  AND NOT EXISTS (
                      SELECT 1
                      FROM xgb_dashopnvpn."UserIdentityLinks" l2
                      WHERE l2."UserId" = l."UserId"
                        AND l2."Id" <> l."Id"
                  );
                """);
        }
    }
}

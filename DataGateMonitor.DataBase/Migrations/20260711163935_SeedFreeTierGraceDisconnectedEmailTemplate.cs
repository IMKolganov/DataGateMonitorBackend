using DataGateMonitor.Models.EmailTemplates;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class SeedFreeTierGraceDisconnectedEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var epoch = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "EmailBroadcastTemplates",
                columns: ["Name", "Description", "Subject", "BodyHtml", "CreatedByUserId", "CreateDate", "LastUpdate"],
                values: new object[,]
                {
                    {
                        SystemEmailTemplateNames.FreeTierGraceDisconnected,
                        "Built-in: sent when a Free/Default user is disconnected after their grace period expires. Placeholders: {{PLAN_NAME}}, {{REQUIRED_CHANNEL}}",
                        TransactionalEmailHtml.DefaultFreeTierGraceDisconnectedSubject,
                        TransactionalEmailHtml.BuildFreeTierGraceDisconnectedWithPlaceholders(),
                        null,
                        epoch,
                        epoch
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM xgb_dashopnvpn."EmailBroadcastTemplates"
                WHERE "Name" = 'system.free_tier_grace_disconnected';
                """);
        }
    }
}

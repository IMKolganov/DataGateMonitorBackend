using DataGateMonitor.Models.EmailTemplates;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class SeedTransactionalEmailTemplates : Migration
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
                        SystemEmailTemplateNames.EmailConfirmation,
                        "Built-in: registration email confirmation. Placeholders: {{CODE}}, {{TTL_MINUTES}}",
                        TransactionalEmailHtml.DefaultConfirmationSubject,
                        TransactionalEmailHtml.BuildEmailConfirmationWithPlaceholders(),
                        null,
                        epoch,
                        epoch
                    },
                    {
                        SystemEmailTemplateNames.AdminPasswordReset,
                        "Built-in: administrator password reset one-time code. Placeholders: {{CODE}}, {{TTL_MINUTES}}",
                        TransactionalEmailHtml.DefaultAdminPasswordResetSubject,
                        TransactionalEmailHtml.BuildAdminPasswordResetWithPlaceholders(),
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
                WHERE "Name" IN ('system.email_confirmation','system.admin_password_reset');
                """);
        }
    }
}

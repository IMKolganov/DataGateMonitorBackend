using DataGateMonitor.DataBase.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260721220000_Localization_VpnServerNotAllowedByQuotaPlan")]
    public partial class Localization_VpnServerNotAllowedByQuotaPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                columns: new[] { "Id", "Key", "Language", "Text" },
                values: new object[,]
                {
                    {
                        100,
                        "VpnServerNotAllowedByQuotaPlan",
                        1,
                        "This VPN server is not available on your current plan. Please choose another server or upgrade your plan."
                    },
                    {
                        101,
                        "VpnServerNotAllowedByQuotaPlan",
                        2,
                        "Αυτός ο διακομιστής VPN δεν είναι διαθέσιμος στο τρέχον πρόγραμμά σας. Επιλέξτε άλλον διακομιστή ή αναβαθμίστε το πρόγραμμα."
                    },
                    {
                        102,
                        "VpnServerNotAllowedByQuotaPlan",
                        3,
                        "Этот VPN-сервер недоступен на вашем текущем тарифе. Выберите другой сервер или обновите план."
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 102);
        }
    }
}

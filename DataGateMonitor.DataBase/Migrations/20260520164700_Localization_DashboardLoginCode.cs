using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class Localization_DashboardLoginCode : Migration
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
                    { 73, "DashboardLoginCode", 1, "Your dashboard login code:\n\n<code>{code}</code>\n\nValid for {minutes} min. Enter it on the DataGate Monitor sign-in page under «Continue with Telegram».\nDo not share this code." },
                    { 74, "DashboardLoginCode", 2, "Ο κωδικός σύνδεσης στον πίνακα:\n\n<code>{code}</code>\n\nΙσχύει για {minutes} λεπτά. Εισαγάγετέ τον στη σελίδα σύνδεσης DataGate Monitor στην επιλογή «Continue with Telegram».\nΜην μοιράζεστε τον κωδικό." },
                    { 75, "DashboardLoginCode", 3, "Код для входа в панель:\n\n<code>{code}</code>\n\nДействует {minutes} мин. Введите его на странице входа DataGate Monitor в разделе «Continue with Telegram».\nНе передавайте код никому." },
                    { 76, "DashboardLoginCodeError", 1, "Could not issue a login code. Register in the bot with /register first, or contact support if you are blocked." },
                    { 77, "DashboardLoginCodeError", 2, "Αδυναμία έκδοσης κωδικού. Κάντε πρώτα εγγραφή με /register ή επικοινωνήστε με την υποστήριξη αν έχετε αποκλειστεί." },
                    { 78, "DashboardLoginCodeError", 3, "Не удалось выдать код. Сначала зарегистрируйтесь в боте через /register или обратитесь в поддержку, если аккаунт заблокирован." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 78);
        }
    }
}

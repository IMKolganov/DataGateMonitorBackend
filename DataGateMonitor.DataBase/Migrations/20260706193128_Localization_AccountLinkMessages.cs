using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class Localization_AccountLinkMessages : Migration
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
                    { 79, "AccountLinkTelegramAlreadyLinkedToGoogle", 1, "This Telegram account is already linked to Google account {accountLabel}. Sign in with that Google account in the app." },
                    { 80, "AccountLinkTelegramAlreadyLinkedToGoogle", 2, "Αυτός ο λογαριασμός Telegram είναι ήδη συνδεδεμένος με τον Google λογαριασμό {accountLabel}. Συνδεθείτε στην εφαρμογή με αυτόν τον Google λογαριασμό." },
                    { 81, "AccountLinkTelegramAlreadyLinkedToGoogle", 3, "Этот Telegram уже привязан к Google-аккаунту {accountLabel}. Войдите в приложение под этим Google." },
                    { 82, "AccountLinkSuccess", 1, "Accounts linked successfully. User #{userId}" },
                    { 83, "AccountLinkSuccess", 2, "Οι λογαριασμοί συνδέθηκαν επιτυχώς. Χρήστης #{userId}" },
                    { 84, "AccountLinkSuccess", 3, "Аккаунты успешно связаны. Пользователь #{userId}" },
                    { 85, "AccountLinkAlreadyLinked", 1, "Accounts are already linked." },
                    { 86, "AccountLinkAlreadyLinked", 2, "Οι λογαριασμοί είναι ήδη συνδεδεμένοι." },
                    { 87, "AccountLinkAlreadyLinked", 3, "Аккаунты уже связаны." },
                    { 88, "AccountLinkEnterCodePrompt", 1, "Enter the code from the app:\n/link_account CODE\n\nOr send the 8-character code alone in this chat." },
                    { 89, "AccountLinkEnterCodePrompt", 2, "Εισαγάγετε τον κωδικό από την εφαρμογή:\n/link_account CODE\n\nΉ στείλτε μόνο τους 8 χαρακτήρες σε αυτή τη συνομιλία." },
                    { 90, "AccountLinkEnterCodePrompt", 3, "Введите код из приложения:\n/link_account КОД\n\nИли отправьте 8 символов кода отдельным сообщением в этот чат." },
                    { 91, "AccountLinkInvalidCodeFormat", 1, "Invalid code format. Expected 8 characters (A-Z, 2-9)." },
                    { 92, "AccountLinkInvalidCodeFormat", 2, "Μη έγκυρη μορφή κωδικού. Αναμένονται 8 χαρακτήρες (A-Z, 2-9)." },
                    { 93, "AccountLinkInvalidCodeFormat", 3, "Неверный формат кода. Нужны 8 символов (A-Z, 2-9)." },
                    { 94, "AccountLinkNotRegistered", 1, "Telegram account is not registered. Use /register in the bot first." },
                    { 95, "AccountLinkNotRegistered", 2, "Ο λογαριασμός Telegram δεν είναι εγγεγραμμένος. Χρησιμοποιήστε πρώτα /register στο bot." },
                    { 96, "AccountLinkNotRegistered", 3, "Telegram-аккаунт не зарегистрирован. Сначала используйте /register в боте." },
                    { 97, "AccountLinkFailed", 1, "Could not link accounts. Check the code and try again." },
                    { 98, "AccountLinkFailed", 2, "Αποτυχία σύνδεσης λογαριασμών. Ελέγξτε τον κωδικό και δοκιμάστε ξανά." },
                    { 99, "AccountLinkFailed", 3, "Не удалось связать аккаунты. Проверьте код и попробуйте снова." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 99);
        }
    }
}

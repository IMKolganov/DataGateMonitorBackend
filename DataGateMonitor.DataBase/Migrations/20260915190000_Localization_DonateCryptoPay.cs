using DataGateMonitor.DataBase.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateMonitor.DataBase.Migrations
{
    /// <summary>
    /// Crypto donation strings (en / el / ru) and /donate on BotMenu.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260915190000_Localization_DonateCryptoPay")]
    public partial class Localization_DonateCryptoPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            UpdateBotMenu(
                migrationBuilder,
                1,
                "<b><u>Bot Menu</u></b>:\n/get_my_files - get your files for connecting to the VPN" +
                "\n/make_new_file - create a new file for connecting to the VPN" +
                "\n/delete_selected_file - Delete a specific file" +
                "\n/delete_all_files - Delete all files" +
                "\n/how_to_use - receive information on how to use the VPN" +
                "\n/install_client - get a link to download the OpenVPN client for connecting to the VPN" +
                "\n/about_bot - receive information about this bot" +
                "\n/about_project - receive information about the project" +
                "\n/contacts - receive contacts developer" +
                "\n/donate - send a crypto donation" +
                "\n/change_language - Change your language/Изменить язык/Αλλάξτε τη γλώσσα σας");

            UpdateBotMenu(
                migrationBuilder,
                2,
                "<b><u>Μενού Bot</u></b>:\n/get_my_files - αποκτήστε τα αρχεία σας για σύνδεση στο VPN" +
                "\n/make_new_file - δημιουργήστε ένα νέο αρχείο για σύνδεση στο VPN" +
                "\n/delete_selected_file - Διαγραφή συγκεκριμένου αρχείου" +
                "\n/delete_all_files - Διαγραφή όλων των αρχείων" +
                "\n/how_to_use - λάβετε πληροφορίες για τη χρήση του VPN" +
                "\n/install_client - λάβετε σύνδεσμο για λήψη του OpenVPN client" +
                "\n/about_bot - λάβετε πληροφορίες για αυτό το bot" +
                "\n/about_project - λάβετε πληροφορίες για το έργο" +
                "\n/contacts - λάβετε στοιχεία επικοινωνίας του προγραμματιστή" +
                "\n/donate - στείλτε δωρεά σε κρυπτονομίσματα" +
                "\n/change_language - Change your language/Изменить язык/Αλλάξτε τη γλώσσα σας");

            UpdateBotMenu(
                migrationBuilder,
                3,
                "<b><u>Меню бота</u></b>:\n/get_my_files - получите свои файлы для подключения к VPN" +
                "\n/make_new_file - создайте новый файл для подключения к VPN" +
                "\n/delete_selected_file - Удалить выбранный файл" +
                "\n/delete_all_files - Удалить все файлы" +
                "\n/how_to_use - получите информацию о том, как использовать VPN" +
                "\n/install_client - получите ссылку для загрузки клиента OpenVPN" +
                "\n/about_bot - информация об этом боте" +
                "\n/about_project - информация о проекте" +
                "\n/contacts - контакты разработчика" +
                "\n/donate - отправить крипто-донат" +
                "\n/change_language - Change your language/Изменить язык/Αλλάξτε τη γλώσσα σας");

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                columns: new[] { "Id", "Key", "Language", "Text" },
                values: new object[,]
                {
                    {
                        109,
                        "DonateIntro",
                        1,
                        "If DataGate is useful to you, you can send a crypto donation. This is voluntary support — it does not change your VPN plan.\n\nChoose an amount in USD. You can pay in USDT, TON or BTC via @CryptoBot."
                    },
                    {
                        110,
                        "DonateIntro",
                        2,
                        "Αν το DataGate σας είναι χρήσιμο, μπορείτε να στείλετε δωρεά σε κρυπτονομίσματα. Είναι εθελοντική υποστήριξη — το πλάνο VPN δεν αλλάζει.\n\nΕπιλέξτε ποσό σε USD. Πληρωμή σε USDT, TON ή BTC μέσω @CryptoBot."
                    },
                    {
                        111,
                        "DonateIntro",
                        3,
                        "Если DataGate вам помогает, можно отправить крипто-донат. Это добровольная поддержка — тариф VPN не меняется.\n\nВыберите сумму в USD. Оплата в USDT, TON или BTC через @CryptoBot."
                    },
                    { 112, "DonatePayButton", 1, "Pay {amount} USD" },
                    { 113, "DonatePayButton", 2, "Πληρωμή {amount} USD" },
                    { 114, "DonatePayButton", 3, "Оплатить {amount} USD" },
                    {
                        115,
                        "DonateInvoiceCreated",
                        1,
                        "Invoice for {amount} USD is ready. Pay in the @CryptoBot wallet — this chat will thank you after payment."
                    },
                    {
                        116,
                        "DonateInvoiceCreated",
                        2,
                        "Το τιμολόγιο για {amount} USD είναι έτοιμο. Πληρώστε στο πορτοφόλι @CryptoBot — μετά την πληρωμή θα λάβετε ευχαριστήριο εδώ."
                    },
                    {
                        117,
                        "DonateInvoiceCreated",
                        3,
                        "Счёт на {amount} USD готов. Оплатите в кошельке @CryptoBot — после оплаты сюда придёт благодарность."
                    },
                    { 118, "DonateThanks", 1, "Thank you for the {amount} {asset} donation! 💚" },
                    { 119, "DonateThanks", 2, "Ευχαριστούμε για τη δωρεά {amount} {asset}! 💚" },
                    { 120, "DonateThanks", 3, "Спасибо за донат {amount} {asset}! 💚" },
                    { 121, "DonateDisabled", 1, "Crypto donations are not configured yet." },
                    { 122, "DonateDisabled", 2, "Οι δωρεές σε κρυπτονομίσματα δεν έχουν ρυθμιστεί ακόμα." },
                    { 123, "DonateDisabled", 3, "Крипто-донаты пока не настроены." },
                    { 124, "DonateInvoiceFailed", 1, "Could not create a payment invoice. Please try again later." },
                    { 125, "DonateInvoiceFailed", 2, "Δεν ήταν δυνατή η δημιουργία τιμολογίου. Δοκιμάστε ξανά αργότερα." },
                    { 126, "DonateInvoiceFailed", 3, "Не удалось создать счёт. Попробуйте позже." }
                },
                columnTypes: new[] { "integer", "character varying(255)", "integer", "text" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            UpdateBotMenu(
                migrationBuilder,
                1,
                "<b><u>Bot Menu</u></b>:\n/get_my_files - get your files for connecting to the VPN" +
                "\n/make_new_file - create a new file for connecting to the VPN" +
                "\n/delete_selected_file - Delete a specific file" +
                "\n/delete_all_files - Delete all files" +
                "\n/how_to_use - receive information on how to use the VPN" +
                "\n/install_client - get a link to download the OpenVPN client for connecting to the VPN" +
                "\n/about_bot - receive information about this bot" +
                "\n/about_project - receive information about the project" +
                "\n/contacts - receive contacts developer" +
                "\n/change_language - Change your language/Изменить язык/Αλλάξτε τη γλώσσα σας");

            UpdateBotMenu(
                migrationBuilder,
                2,
                "<b><u>Μενού Bot</u></b>:\n/get_my_files - αποκτήστε τα αρχεία σας για σύνδεση στο VPN" +
                "\n/make_new_file - δημιουργήστε ένα νέο αρχείο για σύνδεση στο VPN" +
                "\n/delete_selected_file - Διαγραφή συγκεκριμένου αρχείου" +
                "\n/delete_all_files - Διαγραφή όλων των αρχείων" +
                "\n/how_to_use - λάβετε πληροφορίες για τη χρήση του VPN" +
                "\n/install_client - λάβετε σύνδεσμο για λήψη του OpenVPN client" +
                "\n/about_bot - λάβετε πληροφορίες για αυτό το bot" +
                "\n/about_project - λάβετε πληροφορίες για το έργο" +
                "\n/contacts - λάβετε στοιχεία επικοινωνίας του προγραμματιστή" +
                "\n/change_language - Change your language/Изменить язык/Αλλάξτε τη γλώσσα σας");

            UpdateBotMenu(
                migrationBuilder,
                3,
                "<b><u>Меню бота</u></b>:\n/get_my_files - получите свои файлы для подключения к VPN" +
                "\n/make_new_file - создайте новый файл для подключения к VPN" +
                "\n/delete_selected_file - Удалить выбранный файл" +
                "\n/delete_all_files - Удалить все файлы" +
                "\n/how_to_use - получите информацию о том, как использовать VPN" +
                "\n/install_client - получите ссылку для загрузки клиента OpenVPN" +
                "\n/about_bot - информация об этом боте" +
                "\n/about_project - информация о проекте" +
                "\n/contacts - контакты разработчика" +
                "\n/change_language - Change your language/Изменить язык/Αλλάξτε τη γλώσσα σας");

            for (var id = 109; id <= 126; id++)
            {
                migrationBuilder.DeleteData(
                    schema: "xgb_dashopnvpn",
                    table: "LocalizationTexts",
                    keyColumn: "Id",
                    keyValue: id,
                    keyColumnType: "integer");
            }
        }

        private static void UpdateBotMenu(MigrationBuilder migrationBuilder, int id, string text)
        {
            const string tag = "locmenu";
            var safe = text.Replace("$" + tag + "$", "$ " + tag + " $", StringComparison.Ordinal);
            var quoted = "$" + tag + "$" + safe + "$" + tag + "$";
            migrationBuilder.Sql(
                $"""
                UPDATE xgb_dashopnvpn."LocalizationTexts"
                SET "Text" = {quoted}
                WHERE "Id" = {id};
                """);
        }
    }
}

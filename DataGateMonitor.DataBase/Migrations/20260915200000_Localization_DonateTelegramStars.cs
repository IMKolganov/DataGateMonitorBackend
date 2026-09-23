using DataGateMonitor.DataBase.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <summary>
    /// Stars-first /donate copy and invoice title/description strings.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260915200000_Localization_DonateTelegramStars")]
    public partial class Localization_DonateTelegramStars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            UpdateText(
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
                "\n/donate - support with Telegram Stars" +
                "\n/change_language - Change your language/Изменить язык/Αλλάξτε τη γλώσσα σας");

            UpdateText(
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
                "\n/donate - υποστήριξη με Telegram Stars" +
                "\n/change_language - Change your language/Изменить язык/Αλλάξτε τη γλώσσα σας");

            UpdateText(
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
                "\n/donate - поддержать Telegram Stars" +
                "\n/change_language - Change your language/Изменить язык/Αλλάξτε τη γλώσσα σας");

            UpdateText(
                migrationBuilder,
                109,
                "If DataGate helps you, you can send voluntary support with Telegram Stars. VPN plan does not change.\n\nTap a ⭐ amount — pay inside Telegram.\nUSDT row is only if you already have a @CryptoBot balance.");
            UpdateText(
                migrationBuilder,
                110,
                "Αν το DataGate σας βοηθά, μπορείτε να στείλετε υποστήριξη με Telegram Stars. Το πλάνο VPN δεν αλλάζει.\n\nΠατήστε ποσό ⭐ — πληρωμή μέσα στο Telegram.\nUSDT μόνο αν έχετε ήδη υπόλοιπο στο @CryptoBot.");
            UpdateText(
                migrationBuilder,
                111,
                "Если DataGate вам помогает — добровольная поддержка Telegram Stars. Тариф VPN не меняется.\n\nНажмите сумму ⭐ — оплата внутри Telegram.\nНижний ряд USDT — только если баланс в @CryptoBot уже есть.");

            UpdateText(migrationBuilder, 115, "Invoice for {amount} USD is ready. Pay from an existing @CryptoBot balance.");
            UpdateText(migrationBuilder, 116, "Το τιμολόγιο για {amount} USD είναι έτοιμο. Πληρωμή από υπάρχον υπόλοιπο @CryptoBot.");
            UpdateText(migrationBuilder, 117, "Счёт на {amount} USD готов. Оплата из уже имеющегося баланса @CryptoBot.");

            UpdateText(migrationBuilder, 121, "Donations are turned off right now.");
            UpdateText(migrationBuilder, 122, "Οι δωρεές είναι απενεργοποιημένες.");
            UpdateText(migrationBuilder, 123, "Донаты сейчас выключены.");

            migrationBuilder.InsertData(
                schema: "xgb_dashopnvpn",
                table: "LocalizationTexts",
                columns: new[] { "Id", "Key", "Language", "Text" },
                values: new object[,]
                {
                    { 127, "DonateStarsTitle", 1, "Support DataGate" },
                    { 128, "DonateStarsTitle", 2, "Υποστήριξη DataGate" },
                    { 129, "DonateStarsTitle", 3, "Поддержка DataGate" },
                    { 130, "DonateStarsDescription", 1, "Voluntary support. VPN plan does not change." },
                    { 131, "DonateStarsDescription", 2, "Εθελοντική υποστήριξη. Το πλάνο VPN δεν αλλάζει." },
                    { 132, "DonateStarsDescription", 3, "Добровольная поддержка. Тариф VPN не меняется." }
                },
                columnTypes: new[] { "integer", "character varying(255)", "integer", "text" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (var id = 127; id <= 132; id++)
            {
                migrationBuilder.Sql(
                    $"""
                    DELETE FROM xgb_dashopnvpn."LocalizationTexts"
                    WHERE "Id" = {id};
                    """);
            }

            UpdateText(
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

            UpdateText(
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

            UpdateText(
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

            UpdateText(
                migrationBuilder,
                109,
                "If DataGate is useful to you, you can send a crypto donation. This is voluntary support — it does not change your VPN plan.\n\nChoose an amount in USD. You can pay in USDT, TON or BTC via @CryptoBot.");
            UpdateText(
                migrationBuilder,
                110,
                "Αν το DataGate σας είναι χρήσιμο, μπορείτε να στείλετε δωρεά σε κρυπτονομίσματα. Είναι εθελοντική υποστήριξη — το πλάνο VPN δεν αλλάζει.\n\nΕπιλέξτε ποσό σε USD. Πληρωμή σε USDT, TON ή BTC μέσω @CryptoBot.");
            UpdateText(
                migrationBuilder,
                111,
                "Если DataGate вам помогает, можно отправить крипто-донат. Это добровольная поддержка — тариф VPN не меняется.\n\nВыберите сумму в USD. Оплата в USDT, TON или BTC через @CryptoBot.");

            UpdateText(migrationBuilder, 115, "Invoice for {amount} USD is ready. Pay in the @CryptoBot wallet — this chat will thank you after payment.");
            UpdateText(migrationBuilder, 116, "Το τιμολόγιο για {amount} USD είναι έτοιμο. Πληρώστε στο πορτοφόλι @CryptoBot — μετά την πληρωμή θα λάβετε ευχαριστήριο εδώ.");
            UpdateText(migrationBuilder, 117, "Счёт на {amount} USD готов. Оплатите в кошельке @CryptoBot — после оплаты сюда придёт благодарность.");

            UpdateText(migrationBuilder, 121, "Crypto donations are not configured yet.");
            UpdateText(migrationBuilder, 122, "Οι δωρεές σε κρυπτονομίσματα δεν έχουν ρυθμιστεί ακόμα.");
            UpdateText(migrationBuilder, 123, "Крипто-донаты пока не настроены.");
        }

        private static void UpdateText(MigrationBuilder migrationBuilder, int id, string text)
        {
            const string tag = "locstars";
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

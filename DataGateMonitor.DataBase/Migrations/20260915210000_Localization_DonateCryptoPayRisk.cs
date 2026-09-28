using DataGateMonitor.DataBase.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGateMonitor.DataBase.Migrations
{
    /// <summary>
    /// CryptoBot P2P risk banner before a donation invoice.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260915210000_Localization_DonateCryptoPayRisk")]
    public partial class Localization_DonateCryptoPayRisk : Migration
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
                        133,
                        "DonateCryptoRiskBanner",
                        1,
                        "<b>⚠️ Attention: risk!!!</b>\n\n" +
                        "You chose support via @CryptoBot for {amount} USD.\n\n" +
                        "In many countries, topping up @CryptoBot is done through P2P — a person-to-person transfer. That can expose you to legal and financial risks and problems.\n\n" +
                        "Through P2P you may unwittingly take part in a scheme involving other people's or stolen money, or in other financial or criminal activity.\n\n" +
                        "If you are not sufficiently informed about this payment method, please ignore it. Use @CryptoBot only at your own risk.\n\n" +
                        "Continue only if you already have a @CryptoBot balance and you understand these risks. A donation does not change the VPN plan."
                    },
                    {
                        134,
                        "DonateCryptoRiskBanner",
                        2,
                        "<b>⚠️ Προσοχή: κίνδυνος!!!</b>\n\n" +
                        "Επιλέξατε υποστήριξη μέσω @CryptoBot για {amount} USD.\n\n" +
                        "Σε πολλές χώρες η φόρτιση του @CryptoBot γίνεται με P2P — μεταφορά από άνθρωπο σε άνθρωπο. Αυτό μπορεί να σας εκθέσει σε νομικούς και οικονομικούς κινδύνους.\n\n" +
                        "Μέσω P2P μπορείτε άθελά σας να συμμετάσχετε σε σχήμα με χρήματα τρίτων ή κλεμμένα χρήματα, ή σε άλλη οικονομική ή ποινική υπόθεση.\n\n" +
                        "Αν δεν γνωρίζετε αρκετά αυτόν τον τρόπο πληρωμής, αγνοήστε τον. Χρησιμοποιήστε το @CryptoBot αποκλειστικά με δική σας ευθύνη.\n\n" +
                        "Συνεχίστε μόνο αν έχετε ήδη υπόλοιπο στο @CryptoBot και κατανοείτε τους κινδύνους. Το πλάνο VPN δεν αλλάζει."
                    },
                    {
                        135,
                        "DonateCryptoRiskBanner",
                        3,
                        "<b>⚠️ Внимание: риск!!!</b>\n\n" +
                        "Вы выбрали поддержку через @CryptoBot на {amount} USD.\n\n" +
                        "Пополнение @CryptoBot во многих странах идёт через P2P — перевод от человека человеку. Это может повлечь для вас юридические и финансовые риски и проблемы.\n\n" +
                        "Через P2P вы можете невольно участвовать в схеме с чужими или крадеными деньгами либо в другом финансовом или уголовном деле.\n\n" +
                        "Если вы недостаточно осведомлены об этом способе оплаты — проигнорируйте его. Используйте @CryptoBot только на свой страх и риск.\n\n" +
                        "Продолжайте только если баланс в @CryptoBot у вас уже есть и вы понимаете эти риски. Тариф VPN от доната не меняется."
                    },
                    { 136, "DonateCryptoRiskContinue", 1, "I understand the risk, continue" },
                    { 137, "DonateCryptoRiskContinue", 2, "Κατανοώ τον κίνδυνο, συνέχεια" },
                    { 138, "DonateCryptoRiskContinue", 3, "Понимаю риск, продолжить" },
                    { 139, "DonateCryptoRiskIgnore", 1, "Ignore this method" },
                    { 140, "DonateCryptoRiskIgnore", 2, "Αγνόηση αυτής της μεθόδου" },
                    { 141, "DonateCryptoRiskIgnore", 3, "Игнорировать этот способ" }
                },
                columnTypes: new[] { "integer", "character varying(255)", "integer", "text" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (var id = 133; id <= 141; id++)
            {
                migrationBuilder.Sql(
                    $"""
                    DELETE FROM xgb_dashopnvpn."LocalizationTexts"
                    WHERE "Id" = {id};
                    """);
            }
        }
    }
}

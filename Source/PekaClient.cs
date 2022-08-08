using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;

namespace MieszkanieOswieceniaBot
{
    public sealed class PekaClient
    {
        public PekaClient(string login, string password)
        {
            session = new CookieSession();
            this.login = login; 
            this.password = password;
        }

        public async Task<decimal> GetCurrentBalance()
        {
            string homePageAsString = await GetHomePage();
            if (!homePageAsString.Contains("Saldo"))
            {
                await LogIn();
                homePageAsString = await GetHomePage();
            }

            if (!homePageAsString.Contains("Saldo"))
            {
                return -1;
            }

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(homePageAsString);
            var balanceText = htmlDocument.DocumentNode.Descendants().First(x => x.Id == "clientCards")
                                          .Descendants().First(x => x.Name == "tr" && x.InnerText.Contains("Saldo"))
                                          .Descendants().First(x => x.Name == "td" && x.InnerText.Contains("Kwota")).InnerText;
            var balanceRegex = new Regex(@"Kwota:\s+([-\d,]+)\szł", RegexOptions.Singleline);
            var resultAsText = balanceRegex.Match(balanceText).Groups[1].Value;
            return decimal.Parse(resultAsText, new CultureInfo("pl-PL"));
        }

        private Task<string> GetHomePage()
        {
            return session.Request("https://www.peka.poznan.pl/SOP/account/home.jspb").GetAsync().ReceiveString();
        }

        private async Task LogIn()
        {
            await session.Request("https://www.peka.poznan.pl/SOP/j_spring_security_check").PostUrlEncodedAsync(new { j_username = login, j_password = password });
        
        }

        private readonly CookieSession session;
        private readonly string login;
        private readonly string password;
    }
}

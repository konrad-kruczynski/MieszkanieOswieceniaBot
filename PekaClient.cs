using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Flurl.Http;
using HtmlAgilityPack;

namespace MieszkanieOswieceniaBot
{
    public sealed class PekaClient
    {
        public PekaClient(string login, string password)
        {
            client = new FlurlClient().EnableCookies();
            this.login = login;
            this.password = password;
        }

        public decimal GetCurrentBalance()
        {
            string homePageAsString = GetHomePage();
            if (!homePageAsString.Contains("Saldo"))
            {
                LogIn();
                homePageAsString = GetHomePage();
            }
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(homePageAsString);
            var balanceText = htmlDocument.DocumentNode.Descendants().First(x => x.Id == "clientCards")
                                          .Descendants().First(x => x.Name == "tr" && x.InnerText.Contains("Saldo"))
                                          .Descendants().First(x => x.Name == "td" && x.InnerText.Contains("Kwota")).InnerText;
            var balanceRegex = new Regex(@"Kwota:\s+([-\d,]+) zł", RegexOptions.Singleline);
            var resultAsText = balanceRegex.Match(balanceText).Groups[1].Value;
            return decimal.Parse(resultAsText, new CultureInfo("pl-PL"));
        }

        private string GetHomePage()
        {
            return "https://www.peka.poznan.pl/SOP/account/home.jspb".WithClient(client)
                                                                                     .GetAsync().ReceiveString().Result;
        }

        private void LogIn()
        {
            "https://www.peka.poznan.pl/SOP/j_spring_security_check".WithClient(client)
                                                                    .PostUrlEncodedAsync(new { j_username = login, j_password = password }).Wait();
        }

        private readonly FlurlClient client;
        private readonly string login;
        private readonly string password;
    }
}

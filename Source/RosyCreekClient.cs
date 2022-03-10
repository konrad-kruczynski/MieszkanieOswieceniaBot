using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;

namespace MieszkanieOswieceniaBot
{
    public sealed class RosyCreekClient
    {
        public async Task<(bool Success, string Message)> TryGetNewsAsync()
        {
            var message = string.Empty;

            var pageAsString = await NewsUrl.GetStringAsync();
            var page = new HtmlDocument();
            page.LoadHtml(pageAsString);

            var newestNewsDiv = page.DocumentNode.Descendants().First(x => x.HasClass("news"));
            var dateAsString = newestNewsDiv.SelectSingleNode(@"//h4").InnerText;
            var date = DateTime.ParseExact(dateAsString, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            var header = newestNewsDiv.SelectSingleNode(@"//h3").InnerText;

            var database = Database.Instance;

            string newestShortNews;
            var shortNewsList = page.DocumentNode.Descendants().FirstOrDefault(x => x.Name == "ul" && x.HasClass("short_news_list"));
            if(shortNewsList == null)
            {
                newestShortNews = database.NewestRosyCreekShortNews;
            }
            else
            {
                newestShortNews = RemoveConsecutiveSpacesAndNewlines(shortNewsList.ChildNodes.First(x => x.Name == "li").InnerText);
            }
            
            if(date != database.NewestKnownRosyCreekNewsDate
                || header != database.NewestKnownRosyCreekNewsHeader)
            {
                message += RemoveConsecutiveSpacesAndNewlines(newestNewsDiv.InnerText);
            }

            if(newestShortNews != database.NewestRosyCreekShortNews)
            {
                message +=
                    (message != string.Empty ? Environment.NewLine : string.Empty)
                    + newestShortNews;
            }

            if(message == string.Empty)
            {
                return (false, message);
            }

            database.NewestKnownRosyCreekNewsDate = date;
            database.NewestKnownRosyCreekNewsHeader = header;
            database.NewestRosyCreekShortNews = newestShortNews;

            return (true, message);
        }

        private static string RemoveConsecutiveSpacesAndNewlines(string text)
        {
            var result = new StringBuilder();
            var stringReader = new StringReader(text);
            string line;
            while((line = stringReader.ReadLine()) != null)
            {
                line = RemoveConsecutiveSpacesAndControlChars(line);
                if(line.Length > 0)
                {
                    result.AppendLine(line);
                }
            }

            return result.ToString();
        }

        private static string RemoveConsecutiveSpacesAndControlChars(string text)
        {
            var result = new StringBuilder();
            foreach(var character in text)
            {
                if(char.IsWhiteSpace(character) && result.Length > 0
                    && char.IsWhiteSpace(result[result.Length - 1]))
                {
                    continue;
                }

                if(char.IsControl(character))
                {
                    continue;
                }

                result.Append(character);
            }

            return result.ToString().Trim();
        }

        private const string NewsUrl = "http://usmrozanypotok.pl/pl/news,1";
    }
}

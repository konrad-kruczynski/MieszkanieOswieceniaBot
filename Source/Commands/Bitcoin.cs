using System;
using System.IO;
using System.Threading.Tasks;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Bitcoin : ITextCommand
	{
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            const string currencyFile = "currency.txt";
            if (!File.Exists(currencyFile))
            {
                return "Brak pliku z wielkością portfeli.";
            }
            var btcTask = "https://api.zonda.exchange/rest/trading/stats/BTC-PLN".GetJsonAsync();
            var ltcTask = "https://api.zonda.exchange/rest/trading/stats/LTC-PLN".GetJsonAsync();
            var btcData = await btcTask;
            var ltcData = await ltcTask;
            var currencyFileLines = File.ReadAllLines(currencyFile);
            var btcValue = decimal.Parse(btcData.stats.l) * decimal.Parse(currencyFileLines[0]);
            var originalBtcValue = decimal.Parse(currencyFileLines[1]);
            var ltcValue = decimal.Parse(ltcData.stats.l) * decimal.Parse(currencyFileLines[2]);
            var originalLtcValue = decimal.Parse(currencyFileLines[3]);
            return string.Format("Bitcoin: {0:0.00}PLN ({1:0.#}x)\nLitecoin: {2:0.00}PLN ({3:0.#}x)\nRazem: {4:0.00}PLN  ({5:0.#}x)",
                                 btcValue, btcValue / originalBtcValue, ltcValue, ltcValue / originalLtcValue,
                                 btcValue + ltcValue, (btcValue + ltcValue) / (originalBtcValue + originalLtcValue));
        }
    }
}


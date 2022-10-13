using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Waste : ITextCommand
	{
        public Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            parameters.ExpectNoOtherParameters();

            var wasteEntries = new List<(string, DateTime)>();

            using (var reader = new StreamReader("odpady.csv"))
            {
                var row = reader.ReadLine().Split(';');
                var typesOfWaste = new string[row.Length];
                for (var i = 0; i < typesOfWaste.Length; i++)
                {
                    typesOfWaste[i] = row[i];
                }

                for (var i = 1; i <= 12; i++)
                {
                    row = reader.ReadLine().Split(';');
                    for (var j = 0; j < typesOfWaste.Length; j++)
                    {
                        var days = row[j].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var wasteElements = days.Select(dayNo => (typesOfWaste[j], new DateTime(DateTime.Now.Year, i, int.Parse(dayNo))));
                        wasteEntries.AddRange(wasteElements);
                    }
                }
            }

            var wasteData = wasteEntries.GroupBy(x => x.Item1).ToDictionary(x => x.Key, x => x.Select(y => y.Item2).ToArray());

            static string WasteDateToString(DateTime date)
            {
                if (date == default)
                {
                    return "???";
                }

                return string.Format("{0:ddd dd MMM}", date);
            }

            var result = new StringBuilder();
            foreach (var waste in wasteData)
            {
                var dates = waste.Value;
                var nearestInFuture = dates.Where(x => x > DateTime.Now).OrderBy(x => x - DateTime.Now).FirstOrDefault();
                var nearestInPast = dates.Where(x => x <= DateTime.Now).OrderBy(x => DateTime.Now - x).FirstOrDefault();
                result.AppendFormat("{0}: {1} <-> {2}", waste.Key, WasteDateToString(nearestInPast), WasteDateToString(nearestInFuture));
                result.AppendLine();
            }

            return Task.FromResult(result.ToString());
        }
    }
}


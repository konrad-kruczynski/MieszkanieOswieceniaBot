using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace MieszkanieOswieceniaBot
{
    public class TemperatureDatabase
    {
        public static TemperatureDatabase Instance { get; private set; }

        static TemperatureDatabase()
        {
            Instance = new TemperatureDatabase();
        }

        private TemperatureDatabase()
        {
            
        }

        public void AddSample(TemperatureSample sample)
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var samples = database.GetCollection<TemperatureSample>(CollectionName);
                samples.Insert(sample);
                samples.EnsureIndex(x => x.Date);
            }
        }

        public IEnumerable<TemperatureSample> GetSamples(DateTime startDate, DateTime endDate)
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var samples = database.GetCollection<TemperatureSample>(CollectionName);
                return samples.Find(x => x.Date >= startDate && x.Date <= endDate);
            }
        }

        private const string DatabaseFileName = "temperature.db";
        private const string CollectionName = "samples";

    }
}

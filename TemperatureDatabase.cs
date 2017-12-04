using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Newtonsoft.Json;

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

        public int GetSampleCount()
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                return database.GetCollection(CollectionName).Count();
            }
        }

        public long FileSize
        {
            get
            {
                if(!File.Exists(DatabaseFileName))
                {
                    return 0;
                }
                return new FileInfo(DatabaseFileName).Length;
            }
        }

        public string GetSampleExport(Action<decimal> progressHandler = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var last = stopwatch.Elapsed;
            var exportFileName = "export.jsonz"; // TODO: some tmp file?
            using(var fileStream = File.OpenWrite(exportFileName))
            {
                using(var gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
                {
                    using(var streamWriter = new StreamWriter(gzipStream))
                    {
                        using(var database = new LiteDatabase(DatabaseFileName))
                        {
                            var counter = 0;
                            var collection = database.GetCollection<TemperatureSample>(CollectionName);
                            var allSamplesNumber = collection.Count();
                            foreach(var sample in collection.FindAll())
                            {
                                streamWriter.Write(JsonConvert.SerializeObject(sample));
                                streamWriter.Write(',');
                                streamWriter.WriteLine();
                                if(stopwatch.Elapsed - last < TimeSpan.FromMilliseconds(500))
                                {
                                    progressHandler(1m * counter / allSamplesNumber);
                                    last = stopwatch.Elapsed;
                                }
                                counter++;
                            }
                        }
                    }
                }
            }
            return exportFileName;
        }

        private const string DatabaseFileName = "temperature.db";
        private const string CollectionName = "samples";

    }
}

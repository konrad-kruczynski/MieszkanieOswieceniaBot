using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using LiteDB;
using Newtonsoft.Json;

namespace MieszkanieOswieceniaBot
{
    public class Database
    {
        public static Database Instance { get; private set; }

        static Database()
        {
            Instance = new Database();
        }

        private Database()
        {
            
        }

        public void AddSample<T>(T sample) where T : ISample
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                samples.Insert(sample);
                samples.EnsureIndex(x => x.Date);
            }
        }

        public IEnumerable<T> GetSamples<T>(DateTime startDate, DateTime endDate) where T : ISample
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                return samples.Find(x => x.Date >= startDate && x.Date <= endDate);
            }
        }

        public int GetSampleCount<T>()
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                return database.GetCollection(CollectionNameOfType<T>()).Count();
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

        public string GetTemperatureSampleExport(Action<decimal> progressHandler = null)
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
                            var collection = database.GetCollection<TemperatureSample>(TemperatureCollectionName);
                            var allSamplesNumber = collection.Count();
                            foreach(var sample in collection.FindAll())
                            {
                                streamWriter.Write(JsonConvert.SerializeObject(sample));
                                streamWriter.Write(',');
                                streamWriter.WriteLine();
                                if(stopwatch.Elapsed - last > TimeSpan.FromMilliseconds(300))
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

        private static string CollectionNameOfType<T>()
        {
            var type = typeof(T);
            if(type == typeof(TemperatureSample))
            {
                return TemperatureCollectionName;
            }
            if(type == typeof(StateSample))
            {
                return StateCollectionName;
            }
            throw new InvalidOperationException();
        }

        private const string DatabaseFileName = "temperature.db";
        private const string TemperatureCollectionName = "samples";
        private const string StateCollectionName = "stany";

    }
}

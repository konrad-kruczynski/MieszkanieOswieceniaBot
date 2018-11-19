using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Antmicro.Migrant;
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
            samplesCache = new HashSet<object>();
            serializer = new Serializer(new Antmicro.Migrant.Customization.Settings(disableTypeStamping: true));
            holidayMode = new CachedKeyValue<bool>(this, "holidayMode");
            holidayModeStartedAt = new CachedKeyValue<TimeSpan>(this, "holidayModeStartedAt");
        }

        public void AddSample<T>(T sample) where T : ISample<T>
        {
            var lastSample = samplesCache.OfType<T>().SingleOrDefault();
            if(lastSample != null && lastSample.CanSampleBeSquashed(sample))
            {
                return;
            }
            samplesCache.Remove(lastSample);
            samplesCache.Add(sample);
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                samples.Insert(sample);
                samples.EnsureIndex(x => x.Date);
            }
        }

        public IEnumerable<T> GetSamples<T>(DateTime startDate, DateTime endDate) where T : ISample<T>
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                return samples.Find(x => x.Date >= startDate && x.Date <= endDate);
            }
        }

        public IEnumerable<T> GetAllSamples<T>() where T : ISample<T>
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                return samples.FindAll();
            }
        }

        public IEnumerable<T> GetNewestSamples<T>(int howMany) where T : ISample<T>
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                return samples.Find(Query.All("Date", Query.Descending), 0, howMany).Reverse();
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

        public void Shrink()
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var fileDiskService = new FileDiskService("szrink.tmp");
                database.Engine.Shrink(tempDisk: fileDiskService);
            }
        }

        public bool HolidayMode
        {
            get => holidayMode.Value;
            set => holidayMode.Value = value;
        }

        public TimeSpan HolidayModeStartedAt
        {
            get => holidayModeStartedAt.Value;
            set => holidayModeStartedAt.Value = value;
        }

        private T GetValueByKey<T>(string key)
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var collection = database.GetCollection<KeyValueItem>(KeyValueCollectionName);
                var result = collection.FindOne(x => x.Key == key);
                if(result == null)
                {
                    return default(T);
                }
                var memoryStream = new MemoryStream(result.Value);
                return serializer.Deserialize<T>(memoryStream);
            }
        }

        private void SetValueByKey<T>(string key, T value)
        {
            using(var database = new LiteDatabase(DatabaseFileName))
            {
                var collection = database.GetCollection<KeyValueItem>(KeyValueCollectionName);
                var memoryStream = new MemoryStream();
                serializer.Serialize(value, memoryStream);
                var item = new KeyValueItem { Key = key, Value = memoryStream.ToArray() };
                collection.Upsert(item);
                collection.EnsureIndex(x => x.Key);
            }
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
            if(type == typeof(KeyValueItem))
            {
                return KeyValueCollectionName;
            }
            throw new InvalidOperationException();
        }

        private readonly HashSet<object> samplesCache;
        private readonly Serializer serializer;
        private readonly CachedKeyValue<bool> holidayMode;
        private readonly CachedKeyValue<TimeSpan> holidayModeStartedAt;

        private const string DatabaseFileName = "temperature.db";
        private const string TemperatureCollectionName = "samples";
        private const string StateCollectionName = "stany";
        private const string KeyValueCollectionName = "keyval";

        private class CachedKeyValue<T>
        {
            public CachedKeyValue(Database parent, string name)
            {
                this.name = name;
                this.parent = parent;
                cachedValue = parent.GetValueByKey<T>(name);
            }

            public T Value
            {
                get
                {
                    return cachedValue;
                }
                set
                {
                    cachedValue = value;
                    parent.SetValueByKey(name, value);
                }
            }

            private T cachedValue;
            private readonly string name;
            private readonly Database parent;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Migrantoid;
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
            serializer = new Serializer(new Migrantoid.Customization.Settings(disableTypeStamping: true));
            newestKnownRosyCreekHeader = new CachedKeyValue<string>(this, "newestRosyCreekHeader");
            newestKnownRosyCreekNewsDate = new CachedKeyValue<DateTime>(this, "newestRosyCreekNewsDate");
            newestRosyCreekShortNews = new CachedKeyValue<string>(this, "newestRosyCreekShortNews");
        }

        public void AddSample<T>(T sample) where T : ISample<T>
        {
            var lastComparableSample = samplesCache.OfType<T>().SingleOrDefault(x => x.IsSampleComparable(sample));
            if(lastComparableSample != null && lastComparableSample.CanSampleBeSquashed(sample))
            {
                return;
            }

            samplesCache.Remove(lastComparableSample);
            samplesCache.Add(sample);

            using(var database = new LiteDatabase(ConnectionString))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                if (samples.FindOne(Query.All()) != null)
                {
                    samples.EnsureIndex(x => x.Date);
                }
                samples.Insert(sample);
            }
        }

        public IEnumerable<T> GetSamples<T>(DateTime startDate, DateTime endDate) where T : ISample<T>
        {
            using(var database = new LiteDatabase(ConnectionString))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                foreach (var sample in samples.Find(x => x.Date >= startDate && x.Date <= endDate))
                {
                    yield return sample;
                }
            }
        }

        public IEnumerable<T> GetAllSamples<T>() where T : ISample<T>
        {
            using(var database = new LiteDatabase(ConnectionString))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                foreach (var sample in samples.FindAll())
                {
                    yield return sample;
                }
            }
        }

        public IEnumerable<RelaySample> GetSamplesForRelay(int id)
        {
            using (var database = new LiteDatabase(ConnectionString))
            {
                var samples = database.GetCollection<RelaySample>(RelayCollectionName);
                foreach (var sample in samples.Find(Query.All("Date", Query.Ascending)).Where(x => x.RelayId == id))
                {
                    yield return sample;
                }
            }
        }

        public IEnumerable<T> GetNewestSamples<T>(int howMany) where T : ISample<T>
        {
            using(var database = new LiteDatabase(ConnectionString))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                return samples.Find(Query.All("Date", Query.Descending), 0, howMany).Reverse().ToArray();
            }
        }

        public IEnumerable<T> TakeNewestSamples<T>() where T : ISample<T>
        {
            using (var database = new LiteDatabase(ConnectionString))
            {
                var samples = database.GetCollection<T>(CollectionNameOfType<T>());
                foreach (var sample in samples.Find(Query.All("Date", Query.Descending)))
                {
                    yield return sample;
                }
            }
        }

        public int GetSampleCount<T>()
        {
            using(var database = new LiteDatabase(ConnectionString))
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

        public async Task<string> GetTemperatureSampleExport(Func<decimal, Task> progressHandler = null)
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
                        using(var database = new LiteDatabase(ConnectionString))
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
                                    await progressHandler(1m * counter / allSamplesNumber);
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

        public DateTime NewestKnownRosyCreekNewsDate
        {
            get => newestKnownRosyCreekNewsDate.Value;
            set => newestKnownRosyCreekNewsDate.Value = value;
        }

        public string NewestKnownRosyCreekNewsHeader
        {
            get => newestKnownRosyCreekHeader.Value;
            set => newestKnownRosyCreekHeader.Value = value;
        }

        public string NewestRosyCreekShortNews
        {
            get => newestRosyCreekShortNews.Value;
            set => newestRosyCreekShortNews.Value = value;
        }

        public void AddHouseCooperativeChatId(long chatId)
        {
            using(var database = new LiteDatabase(ConnectionString))
            {
                var collection = database.GetCollection<DatabaseChatId>(ChatIdColectionName);
                collection.Upsert(new DatabaseChatId { Id = chatId });
            }
        }

        public IEnumerable<long> GetHouseCooperativeChatIds()
        {
            using(var database = new LiteDatabase(ConnectionString))
            {
                var collection = database.GetCollection<DatabaseChatId>(ChatIdColectionName);
                var result = collection.FindAll().Select(x => x.Id).ToArray();
                return result;
            }
        }

        private T GetValueByKey<T>(string key)
        {
            using(var database = new LiteDatabase(ConnectionString))
            {
                var collection = database.GetCollection<KeyValueItem>(KeyValueCollectionName);
                var result = collection.FindOne(x => x.Key == key);
                if(result == null)
                {
                    return default(T);
                }
                var memoryStream = new MemoryStream(result.Value);

                try
                {
                    return serializer.Deserialize<T>(memoryStream);
                }
                catch(InvalidOperationException e)
                {
                    CircularLogger.Instance.Log("Problem during deserialization of {0}: '{1}'. Resetting value.", key, e.Message);
                    return default(T);
                }
            }
        }

        private void SetValueByKey<T>(string key, T value)
        {
            using(var database = new LiteDatabase(ConnectionString))
            {
                var collection = database.GetCollection<KeyValueItem>(KeyValueCollectionName);
                var memoryStream = new MemoryStream();
                serializer.Serialize(value, memoryStream);
                var item = new KeyValueItem { Key = key, Value = memoryStream.ToArray() };
                if (collection.FindOne(Query.All()) != null)
                {
                    collection.EnsureIndex(x => x.Key, true);
                }

                collection.Upsert(item);
            }
        }

        private static string CollectionNameOfType<T>()
        {
            var type = typeof(T);
            if(type == typeof(TemperatureSample))
            {
                return TemperatureCollectionName;
            }
            if(type == typeof(RelaySample))
            {
                return RelayCollectionName;
            }
            if(type == typeof(KeyValueItem))
            {
                return KeyValueCollectionName;
            }
            throw new InvalidOperationException();
        }

        private readonly HashSet<object> samplesCache;
        private readonly Serializer serializer;
        private readonly CachedKeyValue<DateTime> newestKnownRosyCreekNewsDate;
        private readonly CachedKeyValue<string> newestKnownRosyCreekHeader;
        private readonly CachedKeyValue<string> newestRosyCreekShortNews;

        private const string DatabaseFileName = "temperature.db";
        private const string ConnectionString = "Filename=" + DatabaseFileName + ";Upgrade=true";
        private const string TemperatureCollectionName = "samples";
        private const string RelayCollectionName = "stany_nowe";
        private const string KeyValueCollectionName = "keyval";
        private const string ChatIdColectionName = "hcChatIds";

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

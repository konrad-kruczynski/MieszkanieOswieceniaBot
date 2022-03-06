using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.ImageSharp;
using OxyPlot.Series;

namespace MieszkanieOswieceniaBot
{
    public class Charter
    {
        public Charter(string dateTimeFormat = "ddd dd.MM HH:mm")
        {
            this.dateTimeFormat = dateTimeFormat;
        }

        public async Task<string> PrepareChart(DateTime startDate, DateTime endDate, bool oneDay, Func<Step, Task> stepHandler = null,
                                   Func<int, Task> onDataCount = null)
        {
            await stepHandler(Step.RetrievingData);

            var samples = Database.Instance.GetSamples<TemperatureSample>(startDate, endDate);
            var samplesCount = samples.Count();
            await onDataCount(samplesCount);
            await stepHandler(Step.CreatingPlot);
            var plotModel = new PlotModel { Title = "Temperatura" };
            plotModel.Background = OxyColors.White;

            if(oneDay)
            {
                plotModel.Axes.Add(new TimeSpanAxis()
                {
                    Position = AxisPosition.Bottom,
                    MajorGridlineStyle = LineStyle.Solid,
                    Minimum = TimeSpanAxis.ToDouble(startDate.TimeOfDay),
                    Maximum = TimeSpanAxis.ToDouble(startDate.TimeOfDay + TimeSpan.FromDays(1)),
                    MaximumPadding = 0,
                    MinimumPadding = 0,
                    StringFormat = @"hh:mm"
                });
            }
            else
            {
                plotModel.Axes.Add(new DateTimeAxis()
                {
                    Position = AxisPosition.Bottom,
                    MajorGridlineStyle = LineStyle.Solid,
                    Minimum = DateTimeAxis.ToDouble(startDate),
                    Maximum = DateTimeAxis.ToDouble(endDate),
                    MaximumPadding = 0,
                    MinimumPadding = 0,
                    StringFormat = dateTimeFormat
                });
            }

            plotModel.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Dot,
                Minimum = 15,
                Maximum = 33
            });

            if (oneDay)
            {
                var numberOfDays = (int)Math.Round((endDate - startDate).TotalDays);
                for (var i = 0; i < numberOfDays; i++)
                {
                    var serie = new LineSeries();
                    var dayStart = startDate + TimeSpan.FromDays(i);
                    var dayEnd = dayStart + TimeSpan.FromDays(1);
                    serie.Title = dayStart.ToString("dd.MM");
                    var samplesThatDay = samples.Where(x => x.Date < dayEnd && x.Date >= dayStart).OrderBy(x => x.Date);

                    foreach(var sample in samplesThatDay)
                    {
                        var adjustedTimeSpan = sample.Date - TimeSpan.FromDays(i) - startDate + startDate.TimeOfDay;
                        serie.Points.Add(new DataPoint(TimeSpanAxis.ToDouble(adjustedTimeSpan), Convert.ToDouble(sample.Temperature)));
                    }

                    plotModel.Series.Add(serie);
                }
            }
            else
            {
                var serie = new LineSeries();
                foreach(var sample in samples)
                {
                    serie.Points.Add(new DataPoint(DateTimeAxis.ToDouble(sample.Date), Convert.ToDouble(sample.Temperature)));
                }

                plotModel.Series.Add(serie);
            }

            await stepHandler(Step.RenderingImage);
            var pngFile = "chart.png";
            using (var stream = File.Create(pngFile))
            {
                var pngExporter = new PngExporter(1300, 800);
                pngExporter.Export(plotModel, stream);
            }


            return pngFile;
        }
        
        public async Task<string> PrepareHistogram(int[] relayNos, string relayName, Func<Step, Task> stepHandler = null)
        {
            await stepHandler(Step.RetrievingData);
            var samples = Database.Instance.GetAllSamples<RelaySample>();

            var minutesInBucket = 6;
            var bucketsCount = 24 * 60 / minutesInBucket;
            var buckets = new int[relayNos.Length, bucketsCount];
            foreach (var sample in samples)
            {
                for (var i = 0; i < relayNos.Length; i++)
                {
                    var active = sample.State && sample.RelayId == relayNos[i];
                    if (!active)
                    {
                        continue;
                    }

                    var minutesFromDayStart = sample.Date.Hour * 60 + sample.Date.Minute;
                    var bucketNo = minutesFromDayStart / minutesInBucket;
                    buckets[i, bucketNo]++;
                }
            }

            await stepHandler(Step.CreatingPlot);
            var plotModel = new PlotModel { Title = "Histogram " + relayName, };
            plotModel.Background = OxyColors.White;
            plotModel.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                Minimum = 0,
                Maximum = bucketsCount,
                MaximumPadding = 0,
                MinimumPadding = 0,
                LabelFormatter = value => TimeSpan.FromMinutes(minutesInBucket*value).ToString(@"h\:mm"),
            });
            plotModel.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Dot,
                Minimum = 0,
                Maximum = buckets.Cast<int>().Max() + 1
            });
            plotModel.IsLegendVisible = true;

            for(var j = 0; j < relayNos.Length; j++)
            {
                var serie = new LineSeries();
                for(var i = 0; i < bucketsCount; i++)
                {
                    serie.Points.Add(new DataPoint(i, buckets[j, i]));
                }
                plotModel.Series.Add(serie);
            }

            var pngFile = "histogram.png";

            await stepHandler(Step.RenderingImage);
            using (var stream = File.Create(pngFile))
            {
                var pngExporter = new PngExporter(1300, 800);
                pngExporter.Export(plotModel, stream);
            }

            return pngFile;
        }

        private readonly string dateTimeFormat;
    }
}

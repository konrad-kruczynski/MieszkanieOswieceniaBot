using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Svg;

namespace MieszkanieOswieceniaBot
{
    public class Charter
    {
        public Charter(string dateTimeFormat = "ddd dd.MM HH:mm")
        {
            this.dateTimeFormat = dateTimeFormat;
        }

        public string PrepareChart(DateTime startDate, DateTime endDate, bool oneDay, Action<Step> stepHandler = null,
                                   Action<int> onDataCount = null)
        {
            stepHandler(Step.RetrievingData);

            var samples = Database.Instance.GetSamples<TemperatureSample>(startDate, endDate);
            var samplesCount = samples.Count();
            onDataCount(samplesCount);
            stepHandler(Step.CreatingPlot);
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

            var svgFile = "chart.svg";
            var pngFile = "chart.png";

            using (var stream = File.Create(svgFile))
            {
                var svgExporter = new SvgExporter { Width = 1300, Height = 800 };
                svgExporter.Export(plotModel, stream);
            }
            stepHandler(Step.RenderingImage);
            var svgDocument = SvgDocument.Open(svgFile);
            var bitmap = svgDocument.Draw();
            bitmap.Save(pngFile, System.Drawing.Imaging.ImageFormat.Png);

            return pngFile;
        }

        public string PrepareHistogram(int[] relayNos, Action<Step> stepHandler = null,
                                   Action<int> onDataCount = null)
        {
            stepHandler(Step.RetrievingData);
            var samples = Database.Instance.GetAllSamples<RelaySample>();
            var samplesCount = samples.Count();
            onDataCount(samplesCount);

            var minutesInBucket = 10;
            var bucketsCount = 24 * 60 / minutesInBucket;
            var buckets = new int[relayNos.Length, bucketsCount];
            for(var i = 0; i < relayNos.Length; i++)
            {
                foreach(var sample in samples)
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

            stepHandler(Step.CreatingPlot);
            var plotModel = new PlotModel { Title = "Histogram", };
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

            var svgFile = "histogram.svg";
            var pngFile = "histogram.png";

            using(var stream = File.Create(svgFile))
            {
                var svgExporter = new SvgExporter { Width = 1300, Height = 800 };
                svgExporter.Export(plotModel, stream);
            }
            stepHandler(Step.RenderingImage);
            var svgDocument = SvgDocument.Open(svgFile);
            var bitmap = svgDocument.Draw();
            bitmap.Save(pngFile, System.Drawing.Imaging.ImageFormat.Png);

            return pngFile;
        }

        private readonly string dateTimeFormat;
    }
}

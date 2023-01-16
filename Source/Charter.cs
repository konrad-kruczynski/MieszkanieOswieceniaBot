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

        public async Task<MemoryStream> PrepareChart(DateTime startDate, DateTime endDate, bool oneDay, Func<Step, Task> stepHandler = null,
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
            var stream = new MemoryStream();
            var pngExporter = new PngExporter(1300, 800);
            pngExporter.Export(plotModel, stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
        
        public async Task<MemoryStream> PrepareHistogram(List<int> relayNos, Func<Step, Task> stepHandler = null)
        {
            await stepHandler(Step.RetrievingData);

            var minutesInBucket = 6;
            var bucketsCount = 24 * 60 / minutesInBucket;
            var buckets = new int[relayNos.Count, bucketsCount];

            var relayNumberInChart = 0;
            foreach (var relayNo in relayNos)
            {                
                var samplesForRelay = Database.Instance.GetSamplesForRelay(relayNo);
                var formerSample = new RelaySample(relayNo, false);

                foreach (var sample in samplesForRelay)
                {
                    if (sample.CanSampleBeSquashed(formerSample))
                    {
                        continue;
                    }

                    // If current sample has positive state, but former doesn't, then this
                    // marks activation and no samples go to buckets. What's important here is
                    // deactivation - in such case we have to fill buckets accordingly.

                    if (sample.State || !formerSample.State)
                    {
                        formerSample = sample;
                        continue;
                    }

                    var startDateTime = formerSample.Date;
                    var endDateTime = sample.Date;
                    var startingDay = startDateTime.Date;
                    var endingDay = endDateTime.Date;
                    var startingBucket = (int)startDateTime.TimeOfDay.TotalMinutes / minutesInBucket;
                    var endingBucket = (int)endDateTime.TimeOfDay.TotalMinutes / minutesInBucket;
                    var fullDaysInBetween = (int)(endingDay - startingDay).TotalDays - 1;

                    // Yup, fullDaysInBetween can have value -1.

                    if (fullDaysInBetween == -1)
                    {
                        // Same day for turning on and off
                        for (var i = startingBucket; i <= endingBucket; i++)
                        {
                            buckets[relayNumberInChart, i]++;
                        }
                    }
                    else
                    {
                        for (var i = startingBucket; i < bucketsCount; i++)
                        {
                            buckets[relayNumberInChart, i]++;
                        }

                        if (fullDaysInBetween > 0)
                        {
                            for (var i = 0; i < bucketsCount; i++)
                            {
                                buckets[relayNumberInChart, i] += fullDaysInBetween;
                            }
                        }

                        for (var i = 0; i < endingBucket; i++)
                        {
                            buckets[relayNumberInChart, i]++;
                        }
                    }

                    formerSample = sample;
                }

                relayNumberInChart++;
            }

            await stepHandler(Step.CreatingPlot);
            var plotModel = new PlotModel { Title = "Histogram" };
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
            var legend = new OxyPlot.Legends.Legend
            {
                LegendTitle = "Legenda"
            };
            plotModel.Legends.Add(legend);

            for(var j = 0; j < relayNos.Count; j++)
            {
                var serie = new LineSeries
                {
                    Title = Globals.Relays[relayNos[j]].FriendlyName
                };
                for (var i = 0; i < bucketsCount; i++)
                {
                    serie.Points.Add(new DataPoint(i, buckets[j, i]));
                }
                plotModel.Series.Add(serie);
            }

            await stepHandler(Step.RenderingImage);
            var stream = new MemoryStream();
            var pngExporter = new JpegExporter(1300, 800, quality: 100);
            pngExporter.Export(plotModel, stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private readonly string dateTimeFormat;
    }
}

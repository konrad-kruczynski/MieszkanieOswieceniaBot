using System;
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

        public string PrepareChart(DateTime startDate, DateTime endDate, Action<Step> stepHandler = null,
                                   Action<int> onDataCount = null)
        {
            stepHandler(Step.RetrievingData);
            var samples = Database.Instance.GetSamples<TemperatureSample>(startDate, endDate);
            var samplesCount = samples.Count();
            onDataCount(samplesCount);
            stepHandler(Step.CreatingPlot);
            var plotModel = new PlotModel { Title = "Temperatura" };
            plotModel.Background = OxyColors.White;
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
            plotModel.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Dot,
                Minimum = 12,
                Maximum = 30
            });

            var serie = new LineSeries();
            foreach(var sample in samples)
            {
                serie.Points.Add(new DataPoint(DateTimeAxis.ToDouble(sample.Date), Convert.ToDouble(sample.Temperature)));
            }
            plotModel.Series.Add(serie);

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

        private readonly string dateTimeFormat;
    }
}

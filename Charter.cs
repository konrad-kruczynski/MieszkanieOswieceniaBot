using System;
using System.IO;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Svg;

namespace MieszkanieOswieceniaBot
{
    public class Charter
    {
        public string PrepareChart(DateTime startDate, DateTime endDate, Action<Step> stepHandler = null)
        {
            stepHandler(Step.RetrievingData);
            var samples = TemperatureDatabase.Instance.GetSamples(startDate, endDate);
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
                StringFormat = "dd.MM"
            });
            plotModel.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Left,
                Minimum = 15,
                Maximum = 30
            });

            var serie = new LineSeries();
            foreach(var sample in samples)
            {
                serie.Points.Add(new DataPoint(DateTimeAxis.ToDouble(sample.Date), Convert.ToDouble(sample.Temperature)));
            }
            plotModel.Series.Add(serie);

            stepHandler(Step.RenderingImage);

            var svgFile = "chart.svg";
            var pngFile = "char.png";

            using (var stream = File.Create(svgFile))
            {
                var pdfExporter = new SvgExporter { Width = 1200, Height = 800 };
                pdfExporter.Export(plotModel, stream);
            }
            var svgDocument = SvgDocument.Open(svgFile);
            var bitmap = svgDocument.Draw();
            bitmap.Save(pngFile, System.Drawing.Imaging.ImageFormat.Png);

            return pngFile;
        }
    }
}

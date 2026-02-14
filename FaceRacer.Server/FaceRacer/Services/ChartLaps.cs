using ScottPlot;
using ScottPlot.PathStrategies;

namespace FaceRacer.Services
{
    public class ChartLaps
    {
        private readonly HttpClient _httpClient;

        public ChartLaps(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public Stream Graph(double[] lapTimes, bool isDarkMode, string title = null, int width = 600, int height = 400)
        {
            // x positions are the lap numbers 1..n
            var xValues = Enumerable.Range(1, lapTimes.Length).Select(i => (double)i).ToArray();

            var plt = new Plot();

            // DARK mode
            var bgColor = isDarkMode ? Color.FromHex("#1f1f1f") : Colors.White;
            var dataBg = isDarkMode ? Color.FromHex("#181818") : Colors.WhiteSmoke;
            var axisColor = isDarkMode ? Colors.White : Colors.Black;
            var gridColor = isDarkMode ? Color.FromHex("#404040") : Colors.LightGray;
            var legendBg = isDarkMode ? Color.FromHex("#404040") : Colors.White;
            var legendFontColor = isDarkMode ? Colors.White : Colors.Black;
            // backgrounds
            plt.FigureBackground.Color = bgColor;
            plt.DataBackground.Color = dataBg;
            // axis line and tick colors
            plt.Axes.Color(axisColor);
            plt.Grid.MajorLineColor = gridColor;
            // legend styling
            plt.Legend.BackgroundColor = legendBg;
            plt.Legend.FontColor = legendFontColor;
            
            plt.Axes.Title.Label.FontSize = 18;
            plt.Axes.Bottom.Label.FontSize = 14;
            plt.Axes.Left.Label.FontSize = 14;
            plt.Axes.Bottom.TickLabelStyle.FontSize = 12;
            plt.Axes.Left.TickLabelStyle.FontSize = 12;
            plt.Legend.FontSize = 12;
            
            var tickGenX = new ScottPlot.TickGenerators.NumericAutomatic() { IntegerTicksOnly = true };
            plt.Axes.Bottom.TickGenerator = tickGenX;

            // main scatter + line
            var mainPlot = plt.Add.Scatter(xValues, lapTimes);
            mainPlot.LineWidth = 4;
            mainPlot.MarkerSize = 8;
            mainPlot.Color = Colors.DodgerBlue;
            mainPlot.MarkerShape = MarkerShape.FilledCircle;
            //mainPlot.PathStrategy = new CubicSpline();

            // axis labels and title
            plt.XLabel("Lap");
            plt.YLabel("Time (seconds)");
            plt.Title(title ?? "Lap Time Chart");

            // Best / Worst / Average calculations
            var best = lapTimes.Min();
            var bestTimeSpan = TimeSpan.FromSeconds(best);

            var worst = lapTimes.Max();
            var worstTimeSpan = TimeSpan.FromSeconds(worst);
            var avg = lapTimes.Average();

            // indices for best/worst
            var bestIdx = Array.IndexOf(lapTimes, best);
            var worstIdx = Array.IndexOf(lapTimes, worst);

            // add colored markers
            var bestMarker = plt.Add.Scatter([xValues[bestIdx]], new[] { best }, color: Colors.LightGreen);
            bestMarker.LegendText = $@"Fastest {bestTimeSpan:mm\:ss\.fff}";
            bestMarker.MarkerSize = 8;

            var worstMarker = plt.Add.Scatter([xValues[worstIdx]], new[] { worst }, color: Colors.Red);
            worstMarker.LegendText = $@"Slowest {worstTimeSpan:mm\:ss\.fff}";
            worstMarker.MarkerSize = 6;
            
            // add text near best marker
            var lblBest = plt.Add.Text(text: $@"{bestTimeSpan:mm\:ss\.fff}", x: xValues[bestIdx], y: best); 
            lblBest.LabelFontSize = 14;
            lblBest.LabelFontColor = Colors.LightGreen;
            lblBest.LabelStyle.Bold = true;
            lblBest.LabelAlignment = Alignment.MiddleLeft;
            lblBest.OffsetY = 12;
            // If it's the last lap, shift right
            if (bestIdx == lapTimes.Length - 1)
            {
                lblBest.OffsetX = -55;
            }
            else
            {
                lblBest.OffsetX = 5;
            }

            // average line
            var avgLine = plt.Add.HorizontalLine(avg, color: Colors.DarkOrange);
            avgLine.LineStyle = new LineStyle(3, Colors.LightGray, LinePattern.Dashed);
            avgLine.LabelText = $"Avg: {avg:F2} sec";
            avgLine.LabelAlignment = Alignment.MiddleRight;
            avgLine.LabelOffsetX = 31; 

            // legend
            plt.Legend.Alignment = Alignment.UpperRight;

            var ms = new MemoryStream(plt.GetImageBytes(width: width, height: height)); 
            return ms;
        }
    }
}

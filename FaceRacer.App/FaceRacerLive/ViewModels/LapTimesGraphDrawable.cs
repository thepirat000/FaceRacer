namespace FaceRacerLive.ViewModels;

public class LapTimesGraphDrawable : IDrawable
{
    private readonly List<double> _lapTimes;

    private const int DefaultMin = 30;
    private const int DefaultMax = 35;
    private const int DefaultMarkValue = 32;

    public LapTimesGraphDrawable(List<double> lapTimes)
    {
        _lapTimes = lapTimes;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_lapTimes.Count <= 0)
        {
            return;
        }

        var width = dirtyRect.Width;
        var height = dirtyRect.Height;
        var margin = 10f;

        var min = Math.Min(DefaultMin, _lapTimes.Min());
        var max = Math.Max(DefaultMax, _lapTimes.Max());

        var stepX = (_lapTimes.Count > 1) ? (width - 2 * margin) / (_lapTimes.Count - 1) : 0;

        // Draw axes
        canvas.StrokeColor = Colors.Gray;
        canvas.DrawLine(margin, height - margin, width - margin, height - margin); // X axis
        canvas.DrawLine(margin, margin, margin, height - margin); // Y axis

        // Draw min and max values at Y axis
        canvas.FontColor = Colors.Gray;
        canvas.FontSize = 9;
        canvas.DrawString(
            value: min.ToString("0"),
            x: 0,
            y: height - margin - 12,
            width: margin,
            height: 24,
            horizontalAlignment: HorizontalAlignment.Right,
            verticalAlignment: VerticalAlignment.Center);

        canvas.DrawString(
            value: max.ToString("0"),
            x: 0,
            y: margin - 10,
            width: margin,
            height: 24,
            horizontalAlignment: HorizontalAlignment.Right,
            verticalAlignment: VerticalAlignment.Center);

        // Draw horizontal dotted line and label at 32
        if (DefaultMarkValue >= min && DefaultMarkValue <= max)
        {
            float yMark = (float)(height - margin - ((DefaultMarkValue - min) / (max - min) * (height - 2 * margin)));
            canvas.StrokeColor = Colors.DarkGray;
            canvas.StrokeSize = 1;
            canvas.StrokeDashPattern = new float[] { 4, 4 }; // Dotted pattern
            canvas.DrawLine(margin, yMark, width - margin, yMark);
            canvas.StrokeDashPattern = null; // Reset to solid

            // Draw the "32" label at the Y axis
            canvas.FontColor = Colors.DarkGray;
            canvas.FontSize = 9;
            canvas.DrawString(
                value: DefaultMarkValue.ToString("0"),
                x: 0,
                y: yMark - 12,
                width: margin,
                height: 24,
                horizontalAlignment: HorizontalAlignment.Right,
                verticalAlignment: VerticalAlignment.Center);
        }

        // Draw lap times as a line
        canvas.StrokeColor = Colors.MediumPurple;
        canvas.StrokeSize = 2;

        for (var i = 0; i < _lapTimes.Count - 1; i++)
        {
            var x1 = margin + i * stepX;
            var x2 = margin + (i + 1) * stepX;

            var y1 = (float)(height - margin - ((_lapTimes[i] - min) / (max - min) * (height - 2 * margin)));
            var y2 = (float)(height - margin - ((_lapTimes[i + 1] - min) / (max - min) * (height - 2 * margin)));

            canvas.DrawLine(x1, y1, x2, y2);
        }
    }
}
using System.Windows.Media;

namespace Winotch;

public sealed record BatteryVisual(double FillWidth, System.Windows.Media.Brush Brush)
{
    private const double MaxFillWidth = 16;

    public static BatteryVisual FromPercent(int percent)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        var color = clamped < 20
            ? System.Windows.Media.Color.FromRgb(255, 69, 58)
            : clamped < 50
                ? System.Windows.Media.Color.FromRgb(255, 204, 0)
                : System.Windows.Media.Color.FromRgb(50, 215, 75);

        return new BatteryVisual(MaxFillWidth * clamped / 100, new SolidColorBrush(color));
    }
}

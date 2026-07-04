using System.Windows.Media;

namespace Winotch;

public sealed record BatteryVisual(double FillWidth, System.Windows.Media.Brush Brush)
{
    private const double MaxFillWidth = 16;

    public static BatteryVisual FromPercent(int percent, bool isCharging = false)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        var color = isCharging
            ? System.Windows.Media.Color.FromRgb(50, 215, 75)
            : clamped < 20
            ? System.Windows.Media.Color.FromRgb(255, 69, 58)
            : clamped < 50
                ? System.Windows.Media.Color.FromRgb(255, 204, 0)
                : System.Windows.Media.Color.FromRgb(246, 246, 244);

        return new BatteryVisual(MaxFillWidth * clamped / 100, new SolidColorBrush(color));
    }
}

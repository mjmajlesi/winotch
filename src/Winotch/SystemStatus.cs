using System.Windows.Forms;

namespace Winotch;

public static class SystemStatus
{
    public static BatteryInfo GetBattery()
    {
        var power = SystemInformation.PowerStatus;
        var percent = Math.Clamp((int)Math.Round(power.BatteryLifePercent * 100), 0, 100);
        var charging = power.PowerLineStatus == PowerLineStatus.Online;
        return new BatteryInfo(percent, charging);
    }
}

public sealed record BatteryInfo(int Percent, bool IsCharging);

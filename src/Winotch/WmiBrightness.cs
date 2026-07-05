using System.Management;

namespace Winotch;

internal static class WmiBrightness
{
    private const string Prefix = "wmi:";

    public static IEnumerable<BrightnessDisplay> ReadDisplays()
    {
        var displays = new List<BrightnessDisplay>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "root\\wmi",
                "SELECT InstanceName, CurrentBrightness FROM WmiMonitorBrightness WHERE Active = TRUE");
            using var results = searcher.Get();
            foreach (ManagementObject monitor in results)
            {
                using (monitor)
                {
                    var instanceName = monitor["InstanceName"]?.ToString();
                    if (string.IsNullOrWhiteSpace(instanceName))
                    {
                        continue;
                    }

                    var current = Convert.ToInt32(monitor["CurrentBrightness"]);
                    var name = displays.Count == 0 ? "Built-in display" : $"Built-in display {displays.Count + 1}";
                    displays.Add(new BrightnessDisplay(
                        Prefix + instanceName,
                        name,
                        Math.Clamp(current, 0, 100),
                        BrightnessDisplayKind.Internal));
                }
            }
        }
        catch
        {
        }

        return displays;
    }

    public static void SetBrightness(string id, int percent)
    {
        var instanceName = StripPrefix(id);
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            return;
        }

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "root\\wmi",
                "SELECT * FROM WmiMonitorBrightnessMethods");
            using var results = searcher.Get();
            foreach (ManagementObject methods in results)
            {
                using (methods)
                {
                    if (!IsTargetInstance(id, methods["InstanceName"]?.ToString()))
                    {
                        continue;
                    }

                    methods.InvokeMethod("WmiSetBrightness", [1u, (byte)Math.Clamp(percent, 0, 100)]);
                    return;
                }
            }
        }
        catch
        {
        }
    }

    private static string StripPrefix(string value) =>
        value.StartsWith(Prefix, StringComparison.Ordinal) ? value[Prefix.Length..] : "";

    internal static bool IsTargetInstance(string id, string? instanceName)
    {
        var target = StripPrefix(id);
        return !string.IsNullOrWhiteSpace(target) &&
            string.Equals(target, instanceName, StringComparison.Ordinal);
    }
}

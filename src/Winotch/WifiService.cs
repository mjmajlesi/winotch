using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Winotch;

public sealed class WifiService
{
    public async Task<WifiStatus> GetCurrentAsync()
    {
        var output = await RunNetshAsync("wlan", "show", "interfaces");
        var ssid = ReadValue(output, "SSID");
        var signal = ReadValue(output, "Signal");
        return new WifiStatus(ssid, signal);
    }

    public async Task<IReadOnlyList<WifiNetwork>> GetNetworksAsync()
    {
        var output = await RunNetshAsync("wlan", "show", "networks", "mode=bssid");
        var networks = new List<WifiNetwork>();
        string? currentName = null;
        string? currentSignal = null;

        foreach (var rawLine in output.Split(Environment.NewLine))
        {
            var line = rawLine.Trim();
            var ssidMatch = Regex.Match(line, @"^SSID \d+ : (?<name>.+)$");
            if (ssidMatch.Success)
            {
                AddCurrent();
                currentName = ssidMatch.Groups["name"].Value.Trim();
                currentSignal = null;
                continue;
            }

            if (line.StartsWith("Signal", StringComparison.OrdinalIgnoreCase))
            {
                currentSignal = ValueAfterColon(line);
            }
        }

        AddCurrent();
        return networks
            .Where(network => !string.IsNullOrWhiteSpace(network.Name))
            .GroupBy(network => network.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(8)
            .ToList();

        void AddCurrent()
        {
            if (!string.IsNullOrWhiteSpace(currentName))
            {
                networks.Add(new WifiNetwork(currentName, currentSignal ?? ""));
            }
        }
    }

    public async Task<string> ConnectAsync(string profileName)
    {
        var output = await RunNetshAsync("wlan", "connect", $"name={profileName}");
        return output.Contains("completed successfully", StringComparison.OrdinalIgnoreCase)
            ? $"Connecting to {profileName}"
            : $"Windows needs a saved profile for {profileName}.";
    }

    private static string? ReadValue(string output, string name)
    {
        foreach (var rawLine in output.Split(Environment.NewLine))
        {
            var line = rawLine.Trim();
            if (line.StartsWith(name, StringComparison.OrdinalIgnoreCase) &&
                !line.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase))
            {
                var value = ValueAfterColon(line);
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        return null;
    }

    private static string ValueAfterColon(string line)
    {
        var colon = line.IndexOf(':');
        return colon < 0 ? "" : line[(colon + 1)..].Trim();
    }

    private static async Task<string> RunNetshAsync(params string[] arguments)
    {
        using var process = new Process();
        process.StartInfo.FileName = "netsh";
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return output;
    }
}

public sealed record WifiStatus(string? Name, string? Signal)
{
    public string SignalText => string.IsNullOrWhiteSpace(Signal) ? "" : Signal;
}

public sealed record WifiNetwork(string Name, string Signal)
{
    public override string ToString() => string.IsNullOrWhiteSpace(Signal) ? Name : $"{Name} ({Signal})";
}

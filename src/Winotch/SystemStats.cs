namespace Winotch;

public sealed record NetworkCounterSnapshot(string Id, long BytesReceived, long BytesSent);

public sealed record NetworkRates(double DownBytesPerSecond, double UpBytesPerSecond);

public static class NetworkRateCalculator
{
    public static NetworkRates? FromSnapshots(
        IEnumerable<NetworkCounterSnapshot> previous,
        IReadOnlyList<NetworkCounterSnapshot> current,
        TimeSpan elapsed)
    {
        if (current.Count == 0)
        {
            return null;
        }

        if (elapsed.TotalSeconds <= 0)
        {
            return new NetworkRates(0, 0);
        }

        var previousById = previous.ToDictionary(snapshot => snapshot.Id, StringComparer.Ordinal);
        double downBytes = 0;
        double upBytes = 0;
        foreach (var snapshot in current)
        {
            if (!previousById.TryGetValue(snapshot.Id, out var old))
            {
                continue;
            }

            downBytes += PositiveDelta(snapshot.BytesReceived, old.BytesReceived);
            upBytes += PositiveDelta(snapshot.BytesSent, old.BytesSent);
        }

        return new NetworkRates(downBytes / elapsed.TotalSeconds, upBytes / elapsed.TotalSeconds);
    }

    private static long PositiveDelta(long current, long previous) => current >= previous ? current - previous : 0;
}

public sealed record SystemStatRowSnapshot(string ValueText);

public sealed record SystemStatsSnapshot(
    SystemStatRowSnapshot? Cpu,
    SystemStatRowSnapshot? Ram,
    SystemStatRowSnapshot? Network)
{
    public bool HasRows => Cpu is not null || Ram is not null || Network is not null;
}

public static class SystemStatsFormatter
{
    private const double BytesPerKilobyte = 1024;
    private const double BytesPerMegabyte = BytesPerKilobyte * 1024;
    private const double BytesPerGigabyte = BytesPerMegabyte * 1024;

    public static string FormatCpu(double percent) =>
        FormattableString.Invariant($"{Math.Clamp(percent, 0, 100):0}%");

    public static string FormatRam(ulong usedBytes, ulong totalBytes) =>
        FormattableString.Invariant($"{usedBytes / BytesPerGigabyte:0.0} / {totalBytes / BytesPerGigabyte:0.#} GB");

    public static string FormatNetwork(NetworkRates rates) =>
        $"{FormatRate(rates.DownBytesPerSecond)} down \u00B7 {FormatRate(rates.UpBytesPerSecond)} up";

    public static string FormatRate(double bytesPerSecond)
    {
        var value = Math.Max(0, bytesPerSecond);
        if (value < BytesPerKilobyte)
        {
            return FormattableString.Invariant($"{Math.Round(value):0} B/s");
        }

        if (value < BytesPerMegabyte)
        {
            return FormattableString.Invariant($"{value / BytesPerKilobyte:0.#} KB/s");
        }

        return FormattableString.Invariant($"{value / BytesPerMegabyte:0.#} MB/s");
    }
}

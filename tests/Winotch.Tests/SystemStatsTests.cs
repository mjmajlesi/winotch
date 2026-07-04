namespace Winotch.Tests;

public sealed class SystemStatsTests
{
    private const double Kilobyte = 1024;
    private const double Megabyte = Kilobyte * 1024;
    private const double Gigabyte = Megabyte * 1024;

    [Fact]
    public void NetworkRatesUseDeltasAndIgnoreMissingOrNewAdapters()
    {
        var previous = new[]
        {
            new NetworkCounterSnapshot("wifi", 1_000, 2_000),
            new NetworkCounterSnapshot("ethernet", 100, 100)
        };
        var current = new[]
        {
            new NetworkCounterSnapshot("wifi", 2_024, 3_024),
            new NetworkCounterSnapshot("cellular", 9_000, 9_000)
        };

        var rates = Assert.IsType<NetworkRates>(
            NetworkRateCalculator.FromSnapshots(previous, current, TimeSpan.FromSeconds(2)));

        Assert.Equal(512, rates.DownBytesPerSecond);
        Assert.Equal(512, rates.UpBytesPerSecond);
    }

    [Fact]
    public void NetworkRatesTreatCounterResetAsZeroDelta()
    {
        var previous = new[] { new NetworkCounterSnapshot("wifi", 10_000, 5_000) };
        var current = new[] { new NetworkCounterSnapshot("wifi", 9_000, 4_500) };

        var rates = Assert.IsType<NetworkRates>(
            NetworkRateCalculator.FromSnapshots(previous, current, TimeSpan.FromSeconds(1)));

        Assert.Equal(0, rates.DownBytesPerSecond);
        Assert.Equal(0, rates.UpBytesPerSecond);
    }

    [Fact]
    public void NetworkRatesReturnNullWithoutCurrentAdapters()
    {
        var previous = new[] { new NetworkCounterSnapshot("wifi", 10_000, 5_000) };

        var rates = NetworkRateCalculator.FromSnapshots(previous, [], TimeSpan.FromSeconds(1));

        Assert.Null(rates);
    }

    [Theory]
    [InlineData(0, "0 B/s")]
    [InlineData(42, "42 B/s")]
    [InlineData(1023, "1023 B/s")]
    [InlineData(1024, "1 KB/s")]
    [InlineData(1536, "1.5 KB/s")]
    [InlineData(1048576, "1 MB/s")]
    [InlineData(3355443.2, "3.2 MB/s")]
    public void RateFormatterUsesReadableBinaryThresholds(double bytesPerSecond, string expected)
    {
        Assert.Equal(expected, SystemStatsFormatter.FormatRate(bytesPerSecond));
    }

    [Fact]
    public void NetworkFormatterLabelsDirection()
    {
        var rates = new NetworkRates(3.2 * Megabyte, 240 * Kilobyte);

        Assert.Equal("3.2 MB/s down \u00B7 240 KB/s up", SystemStatsFormatter.FormatNetwork(rates));
    }

    [Fact]
    public void RamFormatterRoundsUsedToOneDecimalAndTotalToOneDecimalMax()
    {
        Assert.Equal("11.2 / 16 GB", SystemStatsFormatter.FormatRam(Gib(11.24), Gib(16)));
        Assert.Equal("16.0 / 16 GB", SystemStatsFormatter.FormatRam(Gib(15.97), Gib(16)));
    }

    private static ulong Gib(double value) => (ulong)Math.Round(value * Gigabyte);
}

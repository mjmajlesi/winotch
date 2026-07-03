using System.Windows.Media;

namespace Winotch.Tests;

public class StatusParsingTests
{
    [Fact]
    public void NetshParserReadsConnectedWifiAndSignal()
    {
        var output = """
            There is 1 interface on the system:

                Name                   : Wi-Fi
                State                  : connected
                SSID                   : TELUS1255
                BSSID                  : 00:11:22:33:44:55
                Signal                 : 99%
            """;

        var status = WifiService.ParseCurrentNetsh(output);

        Assert.Equal("TELUS1255", status.Name);
        Assert.Equal("99%", status.Signal);
    }

    [Fact]
    public void ProfileParserTrimsWindowsProfileIndex()
    {
        var status = WifiService.ParseCurrentProfile("TELUS1255 2\r\n");

        Assert.Equal("TELUS1255", status);
    }

    [Fact]
    public void NetworkParserDeduplicatesScannedNetworks()
    {
        var output = """
            SSID 1 : TELUS1255
                Network type            : Infrastructure
                Signal                  : 96%
            SSID 2 : TELUS1255
                Network type            : Infrastructure
                Signal                  : 75%
            SSID 3 : Guest
                Network type            : Infrastructure
                Signal                  : 41%
            """;

        var networks = WifiService.ParseNetworks(output);

        Assert.Collection(
            networks,
            first => Assert.Equal("TELUS1255", first.Name),
            second => Assert.Equal("Guest", second.Name));
    }

    [Theory]
    [InlineData(96, 15.36, 50, 215, 75)]
    [InlineData(49, 7.84, 255, 204, 0)]
    [InlineData(19, 3.04, 255, 69, 58)]
    public void BatteryVisualUsesFillAndThresholdColors(int percent, double expectedWidth, byte red, byte green, byte blue)
    {
        var visual = BatteryVisual.FromPercent(percent);
        var brush = Assert.IsType<SolidColorBrush>(visual.Brush);

        Assert.Equal(expectedWidth, visual.FillWidth, precision: 2);
        Assert.Equal(Color.FromRgb(red, green, blue), brush.Color);
    }
}

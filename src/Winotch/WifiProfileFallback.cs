using System.Text.Json;
using System.Text.RegularExpressions;

namespace Winotch;

internal static class WifiProfileFallback
{
    internal const string Command = """
        $profiles = @(Get-NetConnectionProfile -ErrorAction SilentlyContinue)
        $adapters = @(Get-NetAdapter -ErrorAction SilentlyContinue)
        $rows = foreach ($profile in $profiles) {
          $adapter = $adapters | Where-Object { $_.ifIndex -eq $profile.InterfaceIndex } | Select-Object -First 1
          [PSCustomObject]@{
            Name = $profile.Name
            InterfaceIndex = $profile.InterfaceIndex
            IPv4Connectivity = $profile.IPv4Connectivity
            IPv6Connectivity = $profile.IPv6Connectivity
            NdisPhysicalMedium = if ($adapter) { $adapter.NdisPhysicalMedium } else { $null }
            InterfaceDescription = if ($adapter) { $adapter.InterfaceDescription } else { "" }
          }
        }
        ConvertTo-Json -Compress -InputObject @($rows)
        """;

    internal static string? ParseCurrentProfile(string output)
    {
        if (TryParseCurrentProfileJson(output, out var wirelessProfile))
        {
            return wirelessProfile;
        }

        var profile = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));

        return profile is null ? null : NormalizeProfileName(profile);
    }

    private static bool TryParseCurrentProfileJson(string output, out string? profile)
    {
        profile = null;
        try
        {
            using var document = JsonDocument.Parse(output);
            profile = document.RootElement.ValueKind switch
            {
                JsonValueKind.Array => ParseProfileRows(document.RootElement.EnumerateArray()),
                JsonValueKind.Object => ParseProfileRows([document.RootElement]),
                _ => null
            };

            return document.RootElement.ValueKind is JsonValueKind.Array or JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ParseProfileRows(IEnumerable<JsonElement> rows)
    {
        foreach (var row in rows)
        {
            if (!IsInternetConnected(row) || !IsWirelessAdapter(row))
            {
                continue;
            }

            var name = ReadJsonString(row, "Name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                return NormalizeProfileName(name);
            }
        }

        return null;
    }

    private static bool IsInternetConnected(JsonElement row) =>
        IsInternetConnectivity(ReadJsonString(row, "IPv4Connectivity")) ||
        IsInternetConnectivity(ReadJsonString(row, "IPv6Connectivity"));

    private static bool IsInternetConnectivity(string? value) =>
        string.Equals(value, "Internet", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "4", StringComparison.OrdinalIgnoreCase);

    private static bool IsWirelessAdapter(JsonElement row)
    {
        var physicalMedium = ReadJsonString(row, "NdisPhysicalMedium");
        if (physicalMedium is not null &&
            (physicalMedium.Equals("9", StringComparison.OrdinalIgnoreCase) ||
             Regex.IsMatch(physicalMedium, @"\b(Native\s*)?802[._ -]?11\b", RegexOptions.IgnoreCase)))
        {
            return true;
        }

        var description = ReadJsonString(row, "InterfaceDescription");
        return description is not null &&
            Regex.IsMatch(description, @"\b(802\.11|Wireless|Wi-?Fi|WLAN)\b", RegexOptions.IgnoreCase);
    }

    private static string? ReadJsonString(JsonElement row, string propertyName)
    {
        if (!row.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static string NormalizeProfileName(string profile)
    {
        var match = Regex.Match(profile, @"^(?<name>.+)\s+\d+$");
        return match.Success ? match.Groups["name"].Value.Trim() : profile.Trim();
    }
}

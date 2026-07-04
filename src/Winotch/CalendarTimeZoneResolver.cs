namespace Winotch;

public static class CalendarTimeZoneResolver
{
    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        var value = timeZoneId?.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(value))
        {
            return TimeZoneInfo.Local;
        }

        if (value.Equals("UTC", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Etc/UTC", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("GMT", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Z", StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.Utc;
        }

        if (TryFind(value, out var direct))
        {
            return direct;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(value, out var windowsId) &&
            TryFind(windowsId, out var mapped))
        {
            return mapped;
        }

        return TimeZoneInfo.Local;
    }

    private static bool TryFind(string id, out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
        }
        catch (InvalidTimeZoneException)
        {
        }

        timeZone = TimeZoneInfo.Local;
        return false;
    }
}

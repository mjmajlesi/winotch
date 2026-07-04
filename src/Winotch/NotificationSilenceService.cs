using Microsoft.Win32;

namespace Winotch;

public static class NotificationSilenceService
{
    private const string SettingsKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings";

    public static bool IsSilenced()
    {
        var value = Registry.GetValue(SettingsKey, "NOC_GLOBAL_SETTING_TOASTS_ENABLED", null);
        return IsGloballySilenced(value as int?);
    }

    public static bool IsGloballySilenced(int? globalToastsEnabled) => globalToastsEnabled == 0;
}

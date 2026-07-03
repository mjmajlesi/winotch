# Winotch

Winotch is a native Windows notch overlay. It stays centered at the top of the primary screen and shows time, date, battery, Wi-Fi, volume, and Windows notifications in a compact black shell that expands on hover or notification activity.

## Stack

- C# WPF on `net8.0-windows10.0.19041.0`
- Transparent, topmost desktop window for the notch shell
- Windows Forms power status for battery
- Core Audio COM interop for system volume
- `netsh wlan` for Wi-Fi status, network listing, and saved-profile connect attempts
- `UserNotificationListener` for Windows toast notification access when the OS grants permission

WPF is the first implementation because it gives direct transparent-window and desktop interop support with a simple CLI build/run loop.

## Run

```powershell
dotnet run --project src/Winotch/Winotch.csproj
```

Hover the notch to expand it. The volume slider changes the system master volume. Wi-Fi connect works for saved Windows Wi-Fi profiles.

## Notification Access

Windows requires explicit user permission for notification listener access. Full all-app notification access also requires the Windows User Notification capability in a packaged app manifest. If access is not granted or unavailable in the unpackaged dev build, Winotch shows the OS state in the notification panel instead of pretending notifications are available.

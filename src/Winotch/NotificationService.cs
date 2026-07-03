using Windows.Foundation.Metadata;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace Winotch;

public sealed class NotificationService
{
    public async Task<NotificationSnapshot> ReadAsync()
    {
        if (!ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
        {
            return new NotificationSnapshot("Notification listener is not available on this Windows build.", []);
        }

        try
        {
            var listener = UserNotificationListener.Current;
            var access = listener.GetAccessStatus();
            if (access == UserNotificationListenerAccessStatus.Unspecified)
            {
                access = await listener.RequestAccessAsync();
            }

            if (access != UserNotificationListenerAccessStatus.Allowed)
            {
                return new NotificationSnapshot("Allow notification access in Windows Settings.", []);
            }

            var notifications = await listener.GetNotificationsAsync(NotificationKinds.Toast);
            var items = notifications
                .OrderByDescending(notification => notification.CreationTime)
                .Take(4)
                .Select(ReadNotification)
                .Where(item => item is not null)
                .Cast<NotificationItem>()
                .ToList();

            return new NotificationSnapshot(items.Count == 0 ? "No active toasts" : "Windows notifications", items);
        }
        catch (Exception ex) when (ex is NotImplementedException || (uint)ex.HResult == 0x80004001)
        {
            return new NotificationSnapshot("Notification access needs packaged Windows capability.", []);
        }
        catch (Exception ex)
        {
            return new NotificationSnapshot($"Notification access unavailable: {ex.Message}", []);
        }
    }

    private static NotificationItem? ReadNotification(UserNotification notification)
    {
        var binding = notification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
        var text = binding?.GetTextElements()
            .Select(element => element.Text)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray() ?? [];

        if (text.Length == 0)
        {
            return null;
        }

        var title = text[0];
        var body = text.Length > 1 ? string.Join(" ", text.Skip(1)) : notification.AppInfo.DisplayInfo.DisplayName;
        return new NotificationItem(notification.AppInfo.DisplayInfo.DisplayName, title, body);
    }
}

public sealed record NotificationSnapshot(string Status, IReadOnlyList<NotificationItem> Items);

public sealed record NotificationItem(string App, string Title, string Body)
{
    public override string ToString() => $"{App}: {Title} {Body}";
}

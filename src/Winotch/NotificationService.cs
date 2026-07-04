using Windows.Foundation.Metadata;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace Winotch;

public sealed class NotificationService : IDisposable
{
    private readonly object _gate = new();
    private readonly List<NotificationItem> _liveToasts = [];
    private bool _watchingLiveToasts;

    public event EventHandler? NotificationsChanged;

    public NotificationService()
    {
        try
        {
            System.Windows.Automation.Automation.AddAutomationEventHandler(
                System.Windows.Automation.WindowPattern.WindowOpenedEvent,
                System.Windows.Automation.AutomationElement.RootElement,
                System.Windows.Automation.TreeScope.Children,
                OnWindowOpened);
            _watchingLiveToasts = true;
        }
        catch
        {
            _watchingLiveToasts = false;
        }
    }

    public async Task<NotificationSnapshot> ReadAsync()
    {
        var liveToasts = GetLiveToasts();
        if (liveToasts.Count > 0)
        {
            return new NotificationSnapshot("Live Windows toast watcher", liveToasts);
        }

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

            var status = items.Count == 0 && _watchingLiveToasts
                ? "Watching for live toast pop-ups"
                : "Windows notifications";
            return new NotificationSnapshot(items.Count == 0 ? status : "Windows notifications", items);
        }
        catch (Exception ex) when (ex is NotImplementedException || (uint)ex.HResult == 0x80004001)
        {
            return new NotificationSnapshot(_watchingLiveToasts
                ? "Watching live toasts; packaged capability needed for history."
                : "Notification access needs packaged Windows capability.", []);
        }
        catch (Exception ex)
        {
            return new NotificationSnapshot($"Notification access unavailable: {ex.Message}", []);
        }
    }

    public void Dispose()
    {
        if (!_watchingLiveToasts)
        {
            return;
        }

        System.Windows.Automation.Automation.RemoveAutomationEventHandler(
            System.Windows.Automation.WindowPattern.WindowOpenedEvent,
            System.Windows.Automation.AutomationElement.RootElement,
            OnWindowOpened);
    }

    private async void OnWindowOpened(object sender, System.Windows.Automation.AutomationEventArgs e)
    {
        if (sender is not System.Windows.Automation.AutomationElement element)
        {
            return;
        }

        await Task.Delay(250);
        var item = TryReadLiveToast(element);
        if (item is null)
        {
            return;
        }

        lock (_gate)
        {
            if (_liveToasts.Any(existing => existing.Title == item.Title && existing.Body == item.Body))
            {
                return;
            }

            _liveToasts.Insert(0, item);
            if (_liveToasts.Count > 4)
            {
                _liveToasts.RemoveAt(_liveToasts.Count - 1);
            }
        }

        NotificationsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static NotificationItem? TryReadLiveToast(System.Windows.Automation.AutomationElement element)
    {
        try
        {
            if (element.Current.ProcessId == Environment.ProcessId)
            {
                return null;
            }

            var bounds = element.Current.BoundingRectangle;
            if (bounds.IsEmpty || bounds.Width < 180 || bounds.Width > 760 || bounds.Height < 60 || bounds.Height > 520)
            {
                return null;
            }

            var textElements = element.FindAll(
                System.Windows.Automation.TreeScope.Descendants,
                new System.Windows.Automation.PropertyCondition(
                    System.Windows.Automation.AutomationElement.ControlTypeProperty,
                    System.Windows.Automation.ControlType.Text));

            var texts = textElements
                .Cast<System.Windows.Automation.AutomationElement>()
                .Select(text => text.Current.Name.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Distinct(StringComparer.Ordinal)
                .Take(5)
                .ToArray();

            if (texts.Length < 2)
            {
                return null;
            }

            var app = string.IsNullOrWhiteSpace(element.Current.Name) ? "Windows" : element.Current.Name;
            return new NotificationItem(app, texts[0], string.Join(" ", texts.Skip(1)));
        }
        catch
        {
            return null;
        }
    }

    private IReadOnlyList<NotificationItem> GetLiveToasts()
    {
        lock (_gate)
        {
            return _liveToasts.ToList();
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

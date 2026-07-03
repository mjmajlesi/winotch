using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Winotch;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private readonly AudioService _audio = new();
    private readonly WifiService _wifi = new();
    private readonly NotificationService _notifications = new();
    private bool _expanded;
    private bool _updatingVolume;

    public MainWindow()
    {
        InitializeComponent();
        _clockTimer.Tick += (_, _) => UpdateClock();
        _statusTimer.Tick += async (_, _) => await RefreshStatusAsync();
        _notifications.NotificationsChanged += (_, _) => Dispatcher.Invoke(async () => await RefreshStatusAsync());
        SystemEvents.DisplaySettingsChanged += (_, _) => PlaceWindow();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PlaceWindow();
        UpdateClock();
        _clockTimer.Start();
        _statusTimer.Start();
        await RefreshStatusAsync();
    }

    private void PlaceWindow()
    {
        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
        Top = 0;
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString("h:mm tt");
        DateText.Text = now.ToString("ddd, MMM d");
        LargeTimeText.Text = now.ToString("HH:mm:ss");
        LargeDateText.Text = now.ToString("dddd, MMMM d");
    }

    private async Task RefreshStatusAsync()
    {
        var battery = SystemStatus.GetBattery();
        var batteryVisual = BatteryVisual.FromPercent(battery.Percent);
        BatteryFill.Width = batteryVisual.FillWidth;
        BatteryFill.Background = batteryVisual.Brush;
        BatteryBar.Foreground = batteryVisual.Brush;
        BatteryText.Text = $"{battery.Percent}%";
        BatteryBar.Value = battery.Percent;
        BatteryDetailText.Text = battery.IsCharging ? "Charging" : "On battery";

        var volume = _audio.GetVolume();
        _updatingVolume = true;
        VolumeSlider.Value = volume;
        _updatingVolume = false;
        VolumeText.Text = $"{volume:0}%";

        var wifi = await _wifi.GetCurrentAsync();
        WifiText.Text = wifi.Name is null ? "Offline" : $"{wifi.Name} {wifi.SignalText}";
        var networks = (await _wifi.GetNetworksAsync()).ToList();
        if (networks.Count == 0 && wifi.Name is not null)
        {
            networks.Add(new WifiNetwork(wifi.Name, "Connected"));
        }

        WifiList.ItemsSource = networks;
        WifiStateText.Text = wifi.Name is null
            ? "No connected Wi-Fi"
            : networks.Count == 1 && networks[0].Signal == "Connected"
                ? $"{wifi.Name} connected. Scan needs Windows Location permission."
                : $"Connected to {wifi.Name}";

        var notifications = await _notifications.ReadAsync();
        NotificationStateText.Text = notifications.Status;
        NotificationCountText.Text = notifications.Items.Count.ToString();
        NotificationList.ItemsSource = notifications.Items;
        if (notifications.Items.Count > 0)
        {
            ExpandTemporarily();
        }
    }

    private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) => SetExpanded(true);

    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => SetExpanded(false);

    private void SetExpanded(bool expanded)
    {
        if (_expanded == expanded)
        {
            return;
        }

        _expanded = expanded;
        var width = expanded ? 840 : 680;
        Animate(this, WidthProperty, width);
        Animate(this, LeftProperty, (SystemParameters.PrimaryScreenWidth - width) / 2);
        Animate(NotchShell, WidthProperty, width);
        Animate(NotchShell, HeightProperty, expanded ? 246 : 68);
        Animate(DetailPanel, OpacityProperty, expanded ? 1 : 0);
    }

    private async void ExpandTemporarily()
    {
        SetExpanded(true);
        await Task.Delay(4200);
        if (!IsMouseOver)
        {
            SetExpanded(false);
        }
    }

    private static void Animate(FrameworkElement target, DependencyProperty property, double value)
    {
        var animation = new DoubleAnimation(value, TimeSpan.FromMilliseconds(360))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        target.BeginAnimation(property, animation);
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingVolume || !IsLoaded)
        {
            return;
        }

        _audio.SetVolume((float)e.NewValue);
        VolumeText.Text = $"{e.NewValue:0}%";
    }

    private async void ConnectWifi_Click(object sender, RoutedEventArgs e)
    {
        if (WifiList.SelectedItem is not WifiNetwork network)
        {
            WifiStateText.Text = "Select a saved Wi-Fi profile first.";
            return;
        }

        WifiStateText.Text = await _wifi.ConnectAsync(network.Name);
    }

    protected override void OnClosed(EventArgs e)
    {
        _notifications.Dispose();
        base.OnClosed(e);
    }
}

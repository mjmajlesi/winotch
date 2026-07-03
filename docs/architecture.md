# Winotch Architecture

## Runtime Flow

```mermaid
flowchart TD
    App["WPF App"] --> Window["Transparent Topmost Notch Window"]
    Window --> Clock["Clock Timer"]
    Window --> Status["Status Timer"]
    Status --> Battery["Windows Power Status"]
    Status --> Audio["Core Audio Endpoint Volume"]
    Status --> Wifi["netsh wlan"]
    Status --> Notifications["UserNotificationListener"]
    Battery --> Window
    Audio --> Window
    Wifi --> Window
    Notifications --> Window
```

## UI System

```mermaid
flowchart LR
    Tokens["App.xaml Tokens"] --> Shell["Notch Shell"]
    Tokens --> Chips["Status Chips"]
    Tokens --> Panel["Expanded Panel"]
    Shell --> Compact["Compact State"]
    Shell --> Expanded["Expanded State"]
    Expanded --> Notifications["Notifications"]
    Expanded --> Controls["Volume and Wi-Fi Controls"]
```

## Design Tokens

- `NotchBlack`: shell background
- `NotchPanel`: chip/control background
- `NotchText`: primary text
- `NotchMutedText`: secondary text
- Typography: Segoe UI Variable Text, falling back to Segoe UI
- Icons: Segoe MDL2 Assets

## Motion

The resting notch is a compact top-attached pill. Hover and notification activity expand width, height, and detail opacity with a short ease-out animation, matching the supplied reference direction without adding a custom animation engine.

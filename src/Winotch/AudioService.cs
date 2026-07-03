using System.Runtime.InteropServices;

namespace Winotch;

public sealed class AudioService
{
    private readonly IAudioEndpointVolume? _endpoint;

    public AudioService()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumerator();
            enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out var device);
            var iid = typeof(IAudioEndpointVolume).GUID;
            device.Activate(ref iid, 23, IntPtr.Zero, out var endpoint);
            _endpoint = (IAudioEndpointVolume)endpoint;
        }
        catch
        {
            _endpoint = null;
        }
    }

    public float GetVolume()
    {
        if (_endpoint is null)
        {
            return 0;
        }

        _endpoint.GetMasterVolumeLevelScalar(out var value);
        return Math.Clamp(value * 100, 0, 100);
    }

    public void SetVolume(float value)
    {
        _endpoint?.SetMasterVolumeLevelScalar(Math.Clamp(value / 100, 0, 1), Guid.Empty);
    }

    private enum EDataFlow
    {
        Render
    }

    private enum ERole
    {
        Multimedia = 1
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private sealed class MMDeviceEnumerator;

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int NotImpl1();
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice device);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object endpoint);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr client);
        int UnregisterControlChangeNotify(IntPtr client);
        int GetChannelCount(out int channelCount);
        int SetMasterVolumeLevel(float level, Guid eventContext);
        int SetMasterVolumeLevelScalar(float level, Guid eventContext);
        int GetMasterVolumeLevel(out float level);
        int GetMasterVolumeLevelScalar(out float level);
    }
}

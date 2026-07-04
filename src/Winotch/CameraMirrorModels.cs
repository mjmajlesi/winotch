namespace Winotch;

public enum CameraMirrorPhase
{
    Closed,
    Opening,
    Live,
    Error
}

public enum CameraMirrorErrorKind
{
    None,
    NoCamera,
    CameraInUse
}

public readonly record struct CameraMirrorState(CameraMirrorPhase Phase, CameraMirrorErrorKind Error)
{
    public static CameraMirrorState Closed => new(CameraMirrorPhase.Closed, CameraMirrorErrorKind.None);
    public static CameraMirrorState Opening => new(CameraMirrorPhase.Opening, CameraMirrorErrorKind.None);
    public static CameraMirrorState Live => new(CameraMirrorPhase.Live, CameraMirrorErrorKind.None);

    public string Message => Error switch
    {
        CameraMirrorErrorKind.NoCamera => "No camera available",
        CameraMirrorErrorKind.CameraInUse => "Camera is in use",
        _ => ""
    };
}

public static class CameraMirrorLifecycle
{
    public static CameraMirrorState BeginOpen(CameraMirrorState state) =>
        state.Phase == CameraMirrorPhase.Live || state.Phase == CameraMirrorPhase.Opening
            ? state
            : CameraMirrorState.Opening;

    public static CameraMirrorState MarkLive(CameraMirrorState state) =>
        state.Phase == CameraMirrorPhase.Opening ? CameraMirrorState.Live : state;

    public static CameraMirrorState Fail(CameraMirrorErrorKind error) =>
        new(CameraMirrorPhase.Error, error);

    public static CameraMirrorState Close() => CameraMirrorState.Closed;
}

public readonly record struct CameraMirrorSize(double Width, double Height)
{
    public bool IsUsable => Width > 0 && Height > 0;
}

public readonly record struct CameraMirrorPlacement(double X, double Y, double Width, double Height)
{
    public static CameraMirrorPlacement Empty => new(0, 0, 0, 0);
}

public static class CameraMirrorLayout
{
    public static CameraMirrorPlacement AspectFit(CameraMirrorSize source, CameraMirrorSize bounds)
    {
        if (!source.IsUsable || !bounds.IsUsable)
        {
            return CameraMirrorPlacement.Empty;
        }

        var scale = Math.Min(bounds.Width / source.Width, bounds.Height / source.Height);
        var width = source.Width * scale;
        var height = source.Height * scale;
        return new CameraMirrorPlacement(
            (bounds.Width - width) / 2,
            (bounds.Height - height) / 2,
            width,
            height);
    }
}

public sealed record CameraMirrorFrame(int PixelWidth, int PixelHeight, byte[] BgraPixels)
{
    public int Stride => PixelWidth * 4;
}

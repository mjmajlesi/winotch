namespace Winotch;

public static class ShellAnimationTiming
{
    public const int MotionMilliseconds = 420;
    public const int FadeMilliseconds = 170;
    public const int DetailRevealDelayMilliseconds = 40;
    public const int CollapseGuardMilliseconds = 650;

    public static TimeSpan MotionDuration => TimeSpan.FromMilliseconds(MotionMilliseconds);
    public static TimeSpan FadeDuration => TimeSpan.FromMilliseconds(FadeMilliseconds);
    public static TimeSpan DetailRevealDelay => TimeSpan.FromMilliseconds(DetailRevealDelayMilliseconds);
    public static TimeSpan CollapseGuard => TimeSpan.FromMilliseconds(CollapseGuardMilliseconds);
}

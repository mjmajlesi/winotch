namespace Winotch;

public enum FocusTimerPhase
{
    Focus,
    Break
}

public enum FocusTimerStatus
{
    Stopped,
    Running,
    Paused
}

public enum FocusTimerCompletionKind
{
    FocusComplete,
    BreakComplete
}

public sealed record FocusTimerSettings(TimeSpan FocusDuration, TimeSpan BreakDuration, bool AutoCycle)
{
    public static readonly FocusTimerSettings ShortPreset = new(TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(5), false);
    public static readonly FocusTimerSettings LongPreset = new(TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(10), false);
    public const int MinimumMinutes = 1;
    public const int MaximumMinutes = 180;
    private const int CustomBreakMinutes = 5;

    public bool IsValid => IsValidDuration(FocusDuration) && IsValidDuration(BreakDuration);

    public static bool TryCreateCustom(string text, bool autoCycle, out FocusTimerSettings settings, out string error)
    {
        if (!int.TryParse(text.Trim(), out var minutes) || minutes is < MinimumMinutes or > MaximumMinutes)
        {
            settings = ShortPreset with { AutoCycle = autoCycle };
            error = $"Use {MinimumMinutes} to {MaximumMinutes} minutes.";
            return false;
        }

        settings = new FocusTimerSettings(TimeSpan.FromMinutes(minutes), TimeSpan.FromMinutes(CustomBreakMinutes), autoCycle);
        error = string.Empty;
        return true;
    }

    private static bool IsValidDuration(TimeSpan duration) =>
        duration >= TimeSpan.FromMinutes(MinimumMinutes) &&
        duration <= TimeSpan.FromMinutes(MaximumMinutes);
}

public sealed record FocusTimerSnapshot(
    FocusTimerStatus Status,
    FocusTimerPhase Phase,
    string PhaseLabel,
    string RemainingText,
    double Progress,
    bool AutoCycle);

public sealed record FocusTimerAdvance(FocusTimerState State, IReadOnlyList<FocusTimerCompletion> Completions);

public sealed record FocusTimerCompletion(FocusTimerCompletionKind Kind, TimeSpan BreakDuration)
{
    public string ToastTitle => Kind == FocusTimerCompletionKind.FocusComplete
        ? $"Focus complete \u2014 {FocusTimerFormatter.FormatDuration(BreakDuration)} break"
        : "Break over";
}

public static class FocusTimerFormatter
{
    public static string FormatRemaining(TimeSpan remaining)
    {
        var seconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    public static string FormatDuration(TimeSpan duration)
    {
        var seconds = Math.Max(0, (int)Math.Ceiling(duration.TotalSeconds));
        return $"{seconds / 60}:{seconds % 60:00}";
    }
}

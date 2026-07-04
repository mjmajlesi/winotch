namespace Winotch;

public sealed record FocusTimerState(
    FocusTimerStatus Status,
    FocusTimerPhase Phase,
    TimeSpan FocusDuration,
    TimeSpan BreakDuration,
    bool AutoCycle,
    DateTimeOffset PhaseStartedAtUtc,
    TimeSpan PausedElapsed,
    DateTimeOffset? PausedAtUtc,
    int CompletedFocusCycles)
{
    public static FocusTimerState Stopped { get; } = new(
        FocusTimerStatus.Stopped,
        FocusTimerPhase.Focus,
        TimeSpan.FromMinutes(25),
        TimeSpan.FromMinutes(5),
        false,
        DateTimeOffset.UnixEpoch,
        TimeSpan.Zero,
        null,
        0);

    public bool IsActive => Status is FocusTimerStatus.Running or FocusTimerStatus.Paused;
    private TimeSpan PhaseDuration => Phase == FocusTimerPhase.Focus ? FocusDuration : BreakDuration;

    public static FocusTimerState Start(FocusTimerSettings settings, DateTimeOffset now) =>
        settings.IsValid
            ? new FocusTimerState(
                FocusTimerStatus.Running,
                FocusTimerPhase.Focus,
                settings.FocusDuration,
                settings.BreakDuration,
                settings.AutoCycle,
                now.ToUniversalTime(),
                TimeSpan.Zero,
                null,
                0)
            : Stopped;

    public FocusTimerState Pause(DateTimeOffset now)
    {
        if (Status != FocusTimerStatus.Running)
        {
            return this;
        }

        var advanced = AdvanceTo(now).State;
        return advanced.Status == FocusTimerStatus.Running
            ? advanced with { Status = FocusTimerStatus.Paused, PausedAtUtc = now.ToUniversalTime() }
            : advanced;
    }

    public FocusTimerState Resume(DateTimeOffset now)
    {
        if (Status != FocusTimerStatus.Paused || PausedAtUtc is null)
        {
            return this;
        }

        return this with
        {
            Status = FocusTimerStatus.Running,
            PausedElapsed = PausedElapsed + Positive(now.ToUniversalTime() - PausedAtUtc.Value),
            PausedAtUtc = null
        };
    }

    public FocusTimerState Skip(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return this;
        }

        var advanced = Status == FocusTimerStatus.Running ? AdvanceTo(now).State : this;
        if (!advanced.IsActive)
        {
            return advanced;
        }

        var next = advanced.NextPhase(now.ToUniversalTime());
        return next.State;
    }

    public FocusTimerAdvance AdvanceTo(DateTimeOffset now)
    {
        if (Status != FocusTimerStatus.Running)
        {
            return new FocusTimerAdvance(this, []);
        }

        var state = this;
        var completions = new List<FocusTimerCompletion>();
        var utcNow = now.ToUniversalTime();
        while (state.Status == FocusTimerStatus.Running && state.RemainingAt(utcNow) <= TimeSpan.Zero)
        {
            var next = state.NextPhase(state.PhaseEndUtc());
            completions.Add(next.Completion);
            state = next.State;
        }

        return new FocusTimerAdvance(state, completions);
    }

    public FocusTimerSnapshot SnapshotAt(DateTimeOffset now) => new(
        Status,
        Phase,
        Phase == FocusTimerPhase.Focus ? "Focus" : "Break",
        FocusTimerFormatter.FormatRemaining(RemainingAt(now)),
        ProgressAt(now),
        AutoCycle);

    public TimeSpan RemainingAt(DateTimeOffset now) =>
        Status == FocusTimerStatus.Stopped
            ? TimeSpan.Zero
            : PhaseDuration - ElapsedAt(now);

    public double ProgressAt(DateTimeOffset now)
    {
        if (Status == FocusTimerStatus.Stopped || PhaseDuration <= TimeSpan.Zero)
        {
            return 0;
        }

        return Math.Clamp(ElapsedAt(now).TotalSeconds / PhaseDuration.TotalSeconds, 0, 1);
    }

    private FocusTimerTransition NextPhase(DateTimeOffset nextStartedAtUtc)
    {
        if (Phase == FocusTimerPhase.Focus)
        {
            return new FocusTimerTransition(
                this with
                {
                    Phase = FocusTimerPhase.Break,
                    Status = FocusTimerStatus.Running,
                    PhaseStartedAtUtc = nextStartedAtUtc,
                    PausedElapsed = TimeSpan.Zero,
                    PausedAtUtc = null,
                    CompletedFocusCycles = CompletedFocusCycles + 1
                },
                new FocusTimerCompletion(FocusTimerCompletionKind.FocusComplete, BreakDuration));
        }

        var nextState = AutoCycle
            ? this with
            {
                Phase = FocusTimerPhase.Focus,
                Status = FocusTimerStatus.Running,
                PhaseStartedAtUtc = nextStartedAtUtc,
                PausedElapsed = TimeSpan.Zero,
                PausedAtUtc = null
            }
            : Stopped;
        return new FocusTimerTransition(
            nextState,
            new FocusTimerCompletion(FocusTimerCompletionKind.BreakComplete, BreakDuration));
    }

    private TimeSpan ElapsedAt(DateTimeOffset now)
    {
        var effectiveNow = Status == FocusTimerStatus.Paused && PausedAtUtc is not null
            ? PausedAtUtc.Value
            : now.ToUniversalTime();
        return Positive(effectiveNow - PhaseStartedAtUtc.ToUniversalTime() - PausedElapsed);
    }

    private DateTimeOffset PhaseEndUtc() =>
        PhaseStartedAtUtc.ToUniversalTime() + PausedElapsed + PhaseDuration;

    private static TimeSpan Positive(TimeSpan value) =>
        value <= TimeSpan.Zero ? TimeSpan.Zero : value;

    private sealed record FocusTimerTransition(FocusTimerState State, FocusTimerCompletion Completion);
}

using System;

namespace Petcompanion;

// A temporary work session. It never changes the user's saved reminder/movement preferences.
internal sealed class FocusSession
{
    private DateTimeOffset? _ends;
    private TimeSpan? _pausedRemaining;
    public bool IsActive => _ends != null || _pausedRemaining != null;
    public bool IsPaused => _pausedRemaining != null;
    public TimeSpan Remaining(DateTimeOffset now) => _pausedRemaining ?? (_ends is DateTimeOffset end
        ? TimeSpan.FromSeconds(Math.Max(0, (end - now).TotalSeconds)) : TimeSpan.Zero);
    public void Start(int minutes, DateTimeOffset now) { _pausedRemaining = null; _ends = now.AddMinutes(Math.Clamp(minutes, 1, 180)); }
    public void Pause(DateTimeOffset now) { if (_ends == null) return; _pausedRemaining = Remaining(now); _ends = null; }
    public void Resume(DateTimeOffset now) { if (_pausedRemaining is not TimeSpan remaining) return; _ends = now + remaining; _pausedRemaining = null; }
    public bool CompleteIfDue(DateTimeOffset now)
    {
        if (_ends == null || now < _ends) return false;
        End(); return true;
    }
    public void End() { _ends = null; _pausedRemaining = null; }
    public string Status(DateTimeOffset now)
    {
        if (!IsActive) return "Ready for your next session";
        var remaining = Remaining(now);
        string time = remaining.TotalHours >= 1 ? remaining.ToString(@"h\:mm\:ss") : remaining.ToString(@"mm\:ss");
        return (IsPaused ? "Focus paused - " : "Focus - ") + time + " remaining";
    }
}
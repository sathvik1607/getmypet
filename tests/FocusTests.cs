using System;
using System.Collections.Generic;

namespace Petcompanion;

internal static class FocusTests
{
    public static void Run(Action<bool, string> check)
    {
        var now = new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
        var focus = new FocusSession(); focus.Start(25, now);
        check(focus.IsActive && focus.Remaining(now.AddMinutes(5)) == TimeSpan.FromMinutes(20), "Focus countdown");
        focus.Pause(now.AddMinutes(5));
        check(focus.IsPaused && focus.Remaining(now.AddHours(2)) == TimeSpan.FromMinutes(20), "Locked/paused work does not consume focus time");
        focus.Resume(now.AddHours(2));
        check(!focus.CompleteIfDue(now.AddHours(2).AddMinutes(19)), "Resumed focus does not end early");
        check(focus.CompleteIfDue(now.AddHours(2).AddMinutes(20)) && !focus.IsActive, "Focus completes exactly once");
        check(!focus.CompleteIfDue(now.AddDays(1)), "Focus completion is not repeated");
        focus.Start(15, now); focus.End(); check(!focus.IsActive, "Manual focus end");
        focus.Start(0, now); check(focus.Remaining(now) == TimeSpan.FromMinutes(1), "Focus duration bounds");
        var custom = new CustomReminder { Message = "Look far away", Minutes = 2 };
        var settings = new Settings { CustomReminders = new List<CustomReminder> { custom }, ClickThrough = true, PetId = "owl" };
        var schedule = new Schedule(settings, now);
        check(schedule.Upcoming(settings).Message == custom.Message, "Up-next includes custom reminders");
        var restored = System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(settings))!.Validate();
        check(restored.ClickThrough && restored.PetId == "owl", "Owl and click-through preferences persist");
    }
}
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Petcompanion;

public sealed class Schedule
{
    private readonly Dictionary<Guid, DateTimeOffset> _customDue = new();
    public ReminderRequest BuiltIn(BreakKind kind, Settings s) => new(kind, null,
        kind == BreakKind.Water ? "A moment for water." : "Rise. Stretch. Return stronger.",
        kind == BreakKind.Water ? "Take a sip and breathe." : "Stand up, loosen your shoulders, and take a short walk.", s.Style);
    public ReminderRequest? Next(Settings s, DateTimeOffset now)
    {
        var custom = s.CustomReminders.Where(r => r.Enabled && _customDue.TryGetValue(r.Id, out var due) && due <= now)
            .OrderBy(r => _customDue[r.Id]).FirstOrDefault();
        var kind = Due(now);
        if (custom != null && (kind == null || _customDue[custom.Id] < (kind == BreakKind.Water ? WaterDue : StretchDue)))
            return new(null, custom.Id, "A little reminder", custom.Message, custom.Style);
        return kind is BreakKind k ? BuiltIn(k, s) : null;
    }
    public void Finish(ReminderRequest request, bool snooze, Settings s, DateTimeOffset now)
    {
        if (request.Kind is BreakKind k) Finish(k, snooze, s, now);
        else if (s.CustomReminders.FirstOrDefault(r => r.Id == request.CustomId) is CustomReminder r)
            _customDue[r.Id] = now.AddMinutes(snooze ? 5 : r.Minutes);
        if (WaterDue < now.AddMinutes(2)) WaterDue = now.AddMinutes(2);
        if (StretchDue < now.AddMinutes(2)) StretchDue = now.AddMinutes(2);
        foreach (var id in _customDue.Keys.ToArray())
            if (_customDue[id] < now.AddMinutes(2)) _customDue[id] = now.AddMinutes(2);
    }
    public (string Message, DateTimeOffset Due) Upcoming(Settings settings)
    {
        var next = WaterDue <= StretchDue ? (Message: "Water break", Due: WaterDue) : (Message: "Movement break", Due: StretchDue);
        foreach (var reminder in settings.CustomReminders.Where(r => r.Enabled))
            if (_customDue.TryGetValue(reminder.Id, out var due) && due < next.Due) next = (reminder.Message, due);
        return next;
    }
    public DateTimeOffset WaterDue { get; private set; }
    public DateTimeOffset StretchDue { get; private set; }
    public Schedule(Settings settings, DateTimeOffset now) => Reset(settings, now);
    public void Reset(Settings s, DateTimeOffset now)
    {
        _customDue.Clear();
        foreach (var r in s.CustomReminders.Where(r => r.Enabled)) _customDue[r.Id] = now.AddMinutes(r.Minutes);
        WaterDue = now.AddMinutes(s.WaterMinutes);
        StretchDue = now.AddMinutes(s.StretchMinutes);
    }
    public BreakKind? Due(DateTimeOffset now) => now < WaterDue && now < StretchDue ? null
        : WaterDue <= StretchDue ? BreakKind.Water : BreakKind.Stretch;
    public void Finish(BreakKind kind, bool snooze, Settings s, DateTimeOffset now)
    {
        if (kind == BreakKind.Water) WaterDue = now.AddMinutes(snooze ? 5 : s.WaterMinutes);
        else StretchDue = now.AddMinutes(snooze ? 5 : s.StretchMinutes);
        // Give the user space when water and stretch become due together.
        if (WaterDue < now.AddMinutes(2)) WaterDue = now.AddMinutes(2);
        if (StretchDue < now.AddMinutes(2)) StretchDue = now.AddMinutes(2);
    }
}

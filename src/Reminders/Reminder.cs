using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Petcompanion;

public enum BreakKind { Water, Stretch }
public enum ReminderStyle { FullScreen, Popup }
public sealed record CustomReminder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = "";
    public int Minutes { get; set; } = 60;
    public bool Enabled { get; set; } = true;
    public ReminderStyle Style { get; set; } = ReminderStyle.Popup;
}
public sealed record ReminderRequest(BreakKind? Kind, Guid? CustomId, string Title, string Message, ReminderStyle Style);

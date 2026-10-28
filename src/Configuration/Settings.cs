using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Petcompanion;

public enum Movement { Wander, Follow, Stay }

public sealed record Settings
{
    public string PetId { get; set; } = "dragon";
    public ReminderStyle Style { get; set; } = ReminderStyle.FullScreen;
    public List<CustomReminder> CustomReminders { get; set; } = new();
    public int WaterMinutes { get; set; } = 45;
    public int StretchMinutes { get; set; } = 60;
    public int BreakSeconds { get; set; } = 30;
    public int PetSizePercent { get; set; } = 100;
    public int Volume { get; set; } = 40;
    public bool Muted { get; set; }
    public bool ClickThrough { get; set; }
    public bool QuietHours { get; set; } = true;
    public Movement Movement { get; set; } = Movement.Wander;
    public static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Petcompanion");
    public static string FilePath => Path.Combine(Folder, "settings.json");
    internal static string LegacyFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DragonBreak", "settings.json");

    public Settings Validate() => this with
    {
        PetId = PetCatalog.Find(PetId).Id,
        Style = Enum.IsDefined(Style) ? Style : ReminderStyle.FullScreen,
        CustomReminders = (CustomReminders ?? new()).Where(r => r != null && !string.IsNullOrWhiteSpace(r.Message))
            .DistinctBy(r => r.Id).Select(r => r with { Id = r.Id == Guid.Empty ? Guid.NewGuid() : r.Id,
                Message = r.Message.Trim()[..Math.Min(r.Message.Trim().Length, 240)], Minutes = Math.Clamp(r.Minutes, 1, 10080),
                Style = Enum.IsDefined(r.Style) ? r.Style : ReminderStyle.Popup }).ToList(),
        WaterMinutes = Math.Clamp(WaterMinutes, 1, 240),
        StretchMinutes = Math.Clamp(StretchMinutes, 1, 240),
        BreakSeconds = Math.Clamp(BreakSeconds, 5, 300),
        PetSizePercent = Math.Clamp(PetSizePercent, 50, 200),
        Volume = Math.Clamp(Volume, 0, 100),
        Movement = Enum.IsDefined(Movement) ? Movement : Movement.Wander
    };

    public static Settings Load() => LoadFrom(FilePath, LegacyFilePath);

    internal static Settings LoadFrom(string currentPath, string legacyPath)
    {
        foreach (string path in new[] { currentPath, legacyPath }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try { if (File.Exists(path) && JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) is Settings settings) return settings.Validate(); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
        }
        return new Settings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Folder);
        string temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(Validate(), new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, FilePath, true);
    }

    public bool IsQuiet(DateTimeOffset now) => QuietHours && (now.LocalDateTime.Hour >= 22 || now.LocalDateTime.Hour < 8);
}

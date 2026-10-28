using System;
using System.IO;
using System.Text.Json;

namespace Petcompanion;

internal static class RenameTests
{
    public static void Run(Action<bool, string> check)
    {
        check(typeof(Settings).Assembly.GetName().Name == "Petcompanion", "Renamed assembly identity");
        string tempRoot = Path.GetFullPath(Path.GetTempPath());
        string root = Path.GetFullPath(Path.Combine(tempRoot, "Petcompanion-rename-" + Guid.NewGuid().ToString("N")));
        if (!root.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)) throw new Exception("Unexpected test path");
        Directory.CreateDirectory(root);
        try
        {
            string current = Path.Combine(root, "current.json"), legacy = Path.Combine(root, "legacy.json");
            File.WriteAllText(legacy, JsonSerializer.Serialize(new Settings { PetId = "owl", Volume = 27, ClickThrough = true }));
            var migrated = Settings.LoadFrom(current, legacy);
            check(migrated.PetId == "owl" && migrated.Volume == 27 && migrated.ClickThrough, "Legacy preferences survive rename");
            check(!File.Exists(current) && File.Exists(legacy), "Reading legacy preferences does not overwrite them");
            File.WriteAllText(current, JsonSerializer.Serialize(new Settings { PetId = "cat", Volume = 53 }));
            check(Settings.LoadFrom(current, legacy).PetId == "cat", "New preferences take precedence");
            File.WriteAllText(current, "{broken");
            check(Settings.LoadFrom(current, legacy).PetId == "owl", "Corrupt new settings can recover legacy preferences");
        }
        finally { Directory.Delete(root, true); }
    }
}
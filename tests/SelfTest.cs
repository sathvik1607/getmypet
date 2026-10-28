using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Petcompanion;

internal static class SelfTest
{
    public static int Run()
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }
        try
        {
            var s = new Settings { WaterMinutes = 1, StretchMinutes = 1 };
            Check(new Settings().PetSizePercent == 100, "Default size for existing settings");
            Check(new Settings { PetSizePercent = -1 }.Validate().PetSizePercent == 50 &&
                new Settings { PetSizePercent = 999 }.Validate().PetSizePercent == 200, "Size bounds");
            Check(System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(new Settings { PetSizePercent = 150 }))!.PetSizePercent == 150, "Size persists");
            var now = new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
            WanderTests.Run(Check);
            CatTests.Run(Check);
            InteractionTests.Run(Check);
            OwlTests.Run(Check);
            FocusTests.Run(Check);
            RenameTests.Run(Check);
            var timer = new Schedule(s, now);
            Check(timer.Due(now) == null, "No immediate reminder");
            Check(timer.Due(now.AddMinutes(1)) == BreakKind.Water, "Water wins equal deadlines");
            timer.Finish(BreakKind.Water, true, s, now.AddMinutes(1));
            Check(timer.WaterDue == now.AddMinutes(6), "Five-minute snooze");
            Check(timer.StretchDue == now.AddMinutes(3), "No stacked reminders");
            Check(timer.Due(now.AddMinutes(2)) == null, "Grace period");
            Check(timer.Due(now.AddMinutes(3)) == BreakKind.Stretch, "Stretch remains scheduled");
            timer.Reset(s, now.AddHours(8));
            Check(timer.Due(now.AddHours(8)) == null, "Resume resets elapsed reminders");
            var valid = new Settings { WaterMinutes = -1, StretchMinutes = 999, Volume = 500, BreakSeconds = 0, Movement = (Movement)99 }.Validate();
            Check(valid.WaterMinutes == 1 && valid.StretchMinutes == 240, "Interval bounds");
            Check(valid.Volume == 100 && valid.BreakSeconds == 5 && valid.Movement == Movement.Wander, "Other setting bounds");
            var roundTrip = System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(s));
            Check(roundTrip != null && roundTrip.WaterMinutes == s.WaterMinutes && roundTrip.Style == s.Style && roundTrip.CustomReminders.Count == 0, "Legacy settings round trip");
            var custom = new CustomReminder { Message = "Check the oven", Minutes = 2 };
            var customSettings = new Settings { CustomReminders = new() { custom }, Style = ReminderStyle.Popup };
            var customSchedule = new Schedule(customSettings, now);
            var request = customSchedule.Next(customSettings, now.AddMinutes(2));
            Check(request?.CustomId == custom.Id && request.Style == ReminderStyle.Popup, "Custom reminder due with independent style");
            customSchedule.Finish(request!, true, customSettings, now.AddMinutes(2));
            Check(customSchedule.Next(customSettings, now.AddMinutes(6)) == null, "Custom snooze waits five minutes");
            Check(customSchedule.Next(customSettings, now.AddMinutes(7))?.CustomId == custom.Id, "Custom snooze comes due");
            custom.Enabled = false; customSchedule.Reset(customSettings, now);
            Check(customSchedule.Next(customSettings, now.AddMinutes(3)) == null, "Disabled reminder excluded");
            var restored = System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(customSettings))!.Validate();
            Check(restored.CustomReminders[0].Message == custom.Message && restored.Style == ReminderStyle.Popup, "Custom persistence");
            Check(PetCatalog.All.Select(p => p.Id).Distinct().Count() == PetCatalog.All.Count, "Unique pet IDs");
            foreach (var pet in PetCatalog.All)
            {
                var desktop = pet.CreatePresentation(370, false);
                var reminder = pet.CreatePresentation(500, true);
                Check(!ReferenceEquals(desktop.View, reminder.View) && !ReferenceEquals(desktop.Visual, reminder.Visual), "Independent host and reminder visuals: " + pet.Id);
                desktop.Visual.FacingRight = false; desktop.Visual.Bank = 12;
                Check(!desktop.Visual.FacingRight && desktop.Visual.Bank == 12 && desktop.Visual.PoseUpdates > 0, "Shared movement contract: " + pet.Id);
                foreach (string? audio in new[] { pet.IntroAudio, pet.ReminderAudio(BreakKind.Water), pet.ReminderAudio(BreakKind.Stretch) })
                {
                    if (audio == null) continue;
                    using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, audio));
                    using var reader = new BinaryReader(stream);
                    Check(new string(reader.ReadChars(4)) == "RIFF" && stream.Length > 1000, pet.Id + " valid packaged WAV: " + audio);
                }
            }
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test.log"), $"PASS: {count} checks.\n");
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test.log"), "FAIL: " + ex);
            return 1;
        }
    }
}

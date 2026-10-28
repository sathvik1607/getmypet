using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Petcompanion;

internal static class AudioTest
{
    public static int Run(bool audible = false)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var player = new MediaPlayer { Volume = audible ? .35 : 0 };
        string[] clips = audible ? new[] { PetCatalog.Find("owl").IntroAudio! } : PetCatalog.All
            .SelectMany(p => new[] { p.IntroAudio, p.ReminderAudio(BreakKind.Water), p.ReminderAudio(BreakKind.Stretch) })
            .Where(p => p != null).Cast<string>().Distinct().ToArray();
        int index = 0, opened = 0, exit = 1;
        var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };
        void Finish(bool success, string detail)
        {
            timeout.Stop(); player.Close(); exit = success ? 0 : 1;
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, audible ? "sound-check.log" : "audio-test.log"),
                (success ? "PASS: " : "FAIL: ") + detail + "\n");
            app.Shutdown(exit);
        }
        void Play() { player.Open(new Uri(Path.Combine(AppContext.BaseDirectory, clips[index]))); player.Play(); }
        player.MediaOpened += (_, _) => opened++;
        player.MediaFailed += (_, e) => Finish(false, e.ErrorException.Message);
        player.MediaEnded += (_, _) =>
        {
            index++;
            if (index == clips.Length) Finish(opened == clips.Length, $"{clips.Length} pet/voice clips decoded and played to completion" + (audible ? " at 35% volume. Verify audibility on your output device." : " at muted volume."));
            else Play();
        };
        timeout.Tick += (_, _) => Finish(false, "Audio playback timed out");
        timeout.Start(); Play(); app.Run(); return exit;
    }
}
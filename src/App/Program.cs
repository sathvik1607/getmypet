using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Petcompanion;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);
        if (args.Contains("--self-test")) return SelfTest.Run();
        if (args.Contains("--audio-test")) return AudioTest.Run();
        if (args.Contains("--sound-check")) return AudioTest.Run(true);
        if (args.Contains("--model-preview")) return new Application().Run(new ModelPreview());
        bool preview = args.Any(a => a.StartsWith("--preview", StringComparison.Ordinal)) || args.Contains("--smoke-test");
        // Shared with the previous app name to prevent duplicate companions across versions.
        using var single = new Mutex(true, "Local\\DragonBreak.DesktopPet", out bool first);
        if (!first && !preview)
        {
            MessageBox.Show("Petcompanion is already running. Right-click the pet or its tray icon for settings.", "Petcompanion");
            return 0;
        }
        try
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            using var controller = new PetController(app, preview);
            controller.Start(args);
            return app.Run();
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory(Settings.Folder);
            File.WriteAllText(Path.Combine(Settings.Folder, "error.log"), ex.ToString());
            MessageBox.Show("Petcompanion could not start. Details are in " + Path.Combine(Settings.Folder, "error.log"), "Petcompanion");
            return 1;
        }
    }
}

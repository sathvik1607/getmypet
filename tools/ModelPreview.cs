using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Petcompanion;

internal sealed class ModelPreview : Window
{
    public static string ExportFolder => Path.Combine(AppContext.BaseDirectory, "model-preview");
    public ModelPreview()
    {
        Ui.ApplyTheme(this);
        Title = "Petcompanion · 3D model preview"; Width = 1120; Height = 760;
        MinWidth = 700; MinHeight = 500; Background = Ui.Background;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var model = new DragonModel();
        var root = new Grid { Margin = new Thickness(26) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(Ui.Text("DRAGON / 3D STUDY", 27, Ui.Accent));
        title.Children.Add(Ui.Text("Drag to rotate · scroll to zoom · live articulated geometry", 15, Ui.Muted));
        root.Children.Add(title);
        var stage = new Border { Background = new RadialGradientBrush(Color.FromRgb(255, 249, 238), Color.FromRgb(232, 215, 221)), CornerRadius = new CornerRadius(16), Child = model };
        Grid.SetRow(stage, 1); root.Children.Add(stage);
        double yaw = 0, pitch = .25; Point last = default; bool dragging = false;
        void Camera()
        {
            var p = new Point3D(Math.Sin(yaw) * Math.Cos(pitch) * 8, Math.Sin(pitch) * 8, Math.Cos(yaw) * Math.Cos(pitch) * 8);
            model.Camera.Position = p; model.Camera.LookDirection = new Vector3D(-p.X, -p.Y, -p.Z);
        }
        stage.MouseLeftButtonDown += (_, e) => { dragging = true; last = e.GetPosition(stage); stage.CaptureMouse(); };
        stage.MouseLeftButtonUp += (_, _) => { dragging = false; stage.ReleaseMouseCapture(); };
        stage.MouseMove += (_, e) =>
        {
            if (!dragging) return; Point p = e.GetPosition(stage);
            yaw -= (p.X - last.X) * .009; pitch = Math.Clamp(pitch + (p.Y - last.Y) * .007, -1.3, 1.3); last = p; Camera();
        };
        stage.MouseWheel += (_, e) => model.Camera.Width = Math.Clamp(model.Camera.Width * (e.Delta > 0 ? .88 : 1.14), 3, 14);
        var footer = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
        var buttons = new WrapPanel();
        var pause = Ui.Button("Pause motion", () => { });
        pause.Click += (_, _) => { model.Paused = !model.Paused; pause.Content = model.Paused ? "Resume motion" : "Pause motion"; };
        buttons.Children.Add(pause);
        buttons.Children.Add(Ui.Button("Reset view", () => { yaw = 0; pitch = .25; model.Camera.Width = 6.6; Camera(); }));
        var status = Ui.Text("Simple model preview. GLB export contains a static pose, without a skeleton or animation clips.", 13, Ui.Muted);
        buttons.Children.Add(Ui.Button("Export current pose (.glb)", () =>
        {
            try { string path = Path.Combine(ExportFolder, "Dragon-static.glb"); ModelExport.Save(model.Geometry, path); status.Text = "Saved: " + path; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { status.Text = "Export failed: " + ex.Message; }
        }, true));
        footer.Children.Add(buttons); footer.Children.Add(status); Grid.SetRow(footer, 2); root.Children.Add(footer); Content = root;
        Camera();
        var capture = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        capture.Tick += (_, _) =>
        {
            capture.Stop(); Directory.CreateDirectory(ExportFolder);
            ModelExport.Save(model.Geometry, Path.Combine(ExportFolder, "Dragon-static.glb"));
            var bitmap = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(this);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(ExportFolder, "preview.png")); png.Save(file);
        };
        Loaded += (_, _) => capture.Start(); Closed += (_, _) => capture.Stop();
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Petcompanion;

internal sealed class PetWindow : Window
{
    public bool Dragging { get; private set; }
    private readonly Border _body;
    private FrameworkElement? _presentationView;
    private Point? _pressPoint;
    public IPetVisual Model { get; private set; } = null!;
    public void SetPet(IPetDefinition definition)
    {
        var presentation = definition.CreatePresentation(370, false);
        Model = presentation.Visual;
        _body.ToolTip = Model is IInteractivePetVisual
            ? "Click to pet; click nose to boop; drag to move; right-click for actions; double-click for settings"
            : "Drag to move; right-click for controls; double-click for settings";
        _presentationView = presentation.View;
        _body.Child = new Viewbox { Child = presentation.View, Stretch = Stretch.Uniform };
        Title = "Petcompanion pet";
    }
    public void SetClickThrough(bool enabled)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        int style = GetWindowLong(hwnd, -20);
        SetWindowLong(hwnd, -20, enabled ? style | 0x20 : style & ~0x20);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0037);
        _pressPoint = null;
        _body.ReleaseMouseCapture();
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);

    public void ApplySize(int percent)
    {
        double scale = Math.Clamp(percent, 50, 200) / 100d;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var area = hwnd == IntPtr.Zero ? System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea
            : System.Windows.Forms.Screen.FromHandle(hwnd).WorkingArea;
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice;
        double dpiX = transform?.M11 ?? 1, dpiY = transform?.M22 ?? 1;
        scale = Math.Min(scale, Math.Min(area.Width / (380d * dpiX), area.Height / (380d * dpiY)));
        Width = 380 * scale; Height = 380 * scale;
    }

    public PetWindow(Action settings, Func<ContextMenu> menu, IPetDefinition definition)
    {
        Title = "Petcompanion pet"; Width = 380; Height = 380;
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize; Topmost = true; ShowInTaskbar = false; ShowActivated = false;
        _body = new Border { Background = Brushes.Transparent, Cursor = Cursors.Hand,
            ToolTip = "Drag to move; right-click for controls; double-click for settings" };
        Content = _body;
        SetPet(definition);
        var body = _body;
        body.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2) { _pressPoint = null; body.ReleaseMouseCapture(); settings(); return; }
            _pressPoint = e.GetPosition(this); body.CaptureMouse(); e.Handled = true;
        };
        body.MouseMove += (_, e) =>
        {
            if (_pressPoint is not Point start || e.LeftButton != MouseButtonState.Pressed) return;
            Point now = e.GetPosition(this);
            if (Math.Abs(now.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(now.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            _pressPoint = null; body.ReleaseMouseCapture(); Dragging = true;
            if (Model is IInteractivePetVisual interactive) interactive.CancelInteraction();
            try { DragMove(); } catch (InvalidOperationException) { }
            finally { Dragging = false; }
        };
        body.MouseLeftButtonUp += (_, e) =>
        {
            bool tap = _pressPoint != null; _pressPoint = null; body.ReleaseMouseCapture();
            if (tap && Model is IInteractivePetVisual interactive && _presentationView is { ActualWidth: > 0, ActualHeight: > 0 } view)
            {
                var point = e.GetPosition(view);
                interactive.TryTap(new Point(point.X / view.ActualWidth, point.Y / view.ActualHeight));
            }
        };
        body.LostMouseCapture += (_, _) => _pressPoint = null;
        body.MouseRightButtonUp += (_, _) => { var context = menu(); context.PlacementTarget = body; context.IsOpen = true; };
    }
}

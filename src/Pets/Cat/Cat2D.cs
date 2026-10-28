using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Petcompanion;

// Vector-drawn side-view cat. Articulation is continuous; facing is a horizontal mirror.
internal sealed class Cat2D : FrameworkElement, IGroundPetVisual, IInteractivePetVisual
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private double _time, _sleep, _sit, _groom;
    private bool _facingRight = true;
    public bool FacingRight { get => _facingRight; set { _facingRight = value; InvalidateVisual(); } }
    public double Bank { get; set; }
    public double TravelHeadingDegrees { get; set; }
    public int PoseUpdates { get; private set; } = 1;
    public PetActivity Activity { get; set; } = PetActivity.Walking;
    public bool Reminder { get; set; }
    private readonly PetInteractionState _interaction = new();
    public event Action<PetInteraction>? InteractionStarted;
    public bool IsInteracting => _interaction.Active;
    public PetInteraction CurrentInteraction => _interaction.Kind;
    public void Interact(PetInteraction action) { _interaction.Trigger(action); InteractionStarted?.Invoke(action); InvalidateVisual(); }
    public void CancelInteraction() => _interaction.Cancel();
    private PetActivity EffectiveActivity => CurrentInteraction switch {
        PetInteraction.Nap => PetActivity.Sleeping, PetInteraction.Treat => PetActivity.Grooming,
        PetInteraction.None => Activity, _ => PetActivity.Sitting };
    public bool TryTap(Point point)
    {
        double x = point.X * 360, y = point.Y * 360;
        if (!FacingRight) x = 360 - x;
        double headX = 242 - _sit * 13, headY = 235 + _sleep * 66;
        bool head = Math.Pow((x - headX) / 60, 2) + Math.Pow((y - headY) / 55, 2) <= 1;
        head |= x >= headX - 52 && x <= headX + 50 && y >= headY - 85 && y < headY - 25;
        bool body = x >= 90 && x <= 255 && y >= 250 && y <= 348;
        if (!head && !body) return false;
        Interact(EffectiveActivity == PetActivity.Sleeping ? PetInteraction.Wake
            : head && x > headX + 30 && Math.Abs(y - headY - 12) < 18 ? PetInteraction.Boop : PetInteraction.Pet);
        return true;
    }
    private static readonly Brush Orange = ColorBrush(246, 174, 101), Light = ColorBrush(255, 200, 139),
        Cream = ColorBrush(255, 240, 214), Outline = ColorBrush(113, 73, 52), Stripe = ColorBrush(211, 125, 65),
        Pink = ColorBrush(234, 148, 146), Blush = ColorBrush(245, 170, 151), Eyes = ColorBrush(71, 48, 42);
    private static readonly Pen Edge = Stroke(Outline, 3.4), Fine = Stroke(Outline, 2.3);
    private static readonly Geometry Ears = Geometry.Parse("M -40,-28 Q -54,-53 -38,-83 Q -13,-75 -8,-45 Z M 10,-40 Q 22,-76 39,-81 Q 53,-58 47,-26 Z");
    private static readonly Geometry InnerEars = Geometry.Parse("M -36,-41 Q -43,-58 -35,-69 Q -21,-62 -19,-45 Z M 23,-43 Q 29,-64 37,-68 Q 44,-55 39,-38 Z");
    private static readonly Geometry Tail = Geometry.Parse("M 115,289 C 73,290 61,263 66,239 C 69,220 92,219 94,237");
    private static readonly Geometry SleepingTail = Geometry.Parse("M 111,307 C 72,332 116,348 169,342 Q 215,337 208,319");
    private static readonly Geometry Heart = Geometry.Parse("M 0,6 C -14,-5 -20,13 0,25 C 20,13 14,-5 0,6 Z");

    public Cat2D()
    {
        IsHitTestVisible = false;
        _timer.Tick += (_, _) => { UpdatePose(.033); InvalidateVisual(); };
        Loaded += (_, _) => _timer.Start(); Unloaded += (_, _) => _timer.Stop();
        IsVisibleChanged += (_, _) => { if (IsVisible) _timer.Start(); else _timer.Stop(); };
    }
    internal void UpdatePose(double seconds)
    {
        _time += Math.Max(0, seconds);
        _interaction.Advance(seconds);
        _sleep += ((EffectiveActivity == PetActivity.Sleeping ? 1 : 0) - _sleep) * .14;
        _sit += ((EffectiveActivity is PetActivity.Sitting or PetActivity.Grooming ? 1 : 0) - _sit) * .14;
        _groom += ((EffectiveActivity == PetActivity.Grooming ? 1 : 0) - _groom) * .14;
        PoseUpdates++;
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double scale = Math.Min(ActualWidth, ActualHeight) / 360;
        if (scale <= 0) return;
        dc.PushTransform(new TranslateTransform((ActualWidth - 360 * scale) / 2, (ActualHeight - 360 * scale) / 2));
        dc.PushTransform(new ScaleTransform(scale, scale));
        if (!FacingRight) dc.PushTransform(new ScaleTransform(-1, 1, 180, 180));
        double stride = EffectiveActivity == PetActivity.Walking ? Math.Sin(_time * 8) : 0;
        double bob = EffectiveActivity == PetActivity.Walking ? Math.Abs(stride) * -2.5 : Math.Sin(_time * 1.8) * .8;
        if (CurrentInteraction == PetInteraction.Play) bob -= Math.Max(0, Math.Sin(_time * 6)) * 8;
        double bodyX = 166 + _sit * 17, bodyY = 286 + _sleep * 26 + bob;
        double bodyRx = 67 - _sit * 18 + _sleep * 12, bodyRy = 43 + _sit * 4 - _sleep * 18;
        // Soft ground shadow, independent of body breathing.
        dc.PushOpacity(.10); dc.DrawEllipse(Outline, null, new Point(181, 341), 108, 6); dc.Pop();
        if (_sleep < .5)
        {
            dc.PushTransform(new TranslateTransform(bodyX - bodyRx + 6 - 115, bodyY + 3 - 289));
            dc.PushTransform(new RotateTransform(Math.Sin(_time * 2) * 5, 115, 289));
            dc.DrawGeometry(null, Stroke(Outline, 24), Tail); dc.DrawGeometry(null, Stroke(Orange, 17), Tail);
            dc.DrawEllipse(Stripe, null, new Point(66, 251), 8, 4);
            dc.Pop(); dc.Pop();
        }
        Paw(dc, 134 + stride * 8, 330 - Math.Max(0, -stride) * 7, 17, 10);
        Paw(dc, 206 - stride * 8, 330 - Math.Max(0, stride) * 7, 17, 10);
        dc.DrawEllipse(Orange, Edge, new Point(bodyX, bodyY), bodyRx, bodyRy);
        dc.PushOpacity(.36);
        dc.DrawEllipse(Light, null, new Point(bodyX - 14, bodyY - bodyRy * .35), bodyRx * .65, bodyRy * .44);
        dc.Pop();
        dc.DrawEllipse(Cream, null, new Point(bodyX + 25, bodyY + 9), bodyRx * .44, bodyRy * .58);
        // Rounded tabby markings follow the torso rather than swapping frames.
        for (int i = 0; i < 3; i++)
        {
            double x = bodyX - 33 + i * 22;
            Curve(dc, new Point(x, bodyY - bodyRy + 6), new Point(x - 5, bodyY - bodyRy + 15),
                new Point(x + 1, bodyY - bodyRy + 20 - i * 2), Stroke(Stripe, 6.5));
        }
        if (_sleep > .5)
        {
            dc.DrawGeometry(null, Stroke(Outline, 25), SleepingTail); dc.DrawGeometry(null, Stroke(Light, 18), SleepingTail);
            Paw(dc, 205, 333, 19, 9);
        }
        else
        {
            Paw(dc, 149 - stride * 10, 331 - Math.Max(0, stride) * 8, 20, 11);
            double lift = _groom * (38 + Math.Sin(_time * 5) * 7) + (Reminder ? 24 + Math.Sin(_time * 3) * 8 : 0);
            if (CurrentInteraction == PetInteraction.Play) lift += 27 + Math.Sin(_time * 6) * 14;
            if (CurrentInteraction == PetInteraction.Wake) lift += 12;
            Paw(dc, 222 + stride * 10 + _groom * 9, 331 - Math.Max(0, -stride) * 8 - lift, 18, 12);
        }
        double headX = 242 - _sit * 13, headY = 235 + _sleep * 66 + bob;
        if (CurrentInteraction == PetInteraction.Treat) headY += 35 + Math.Sin(_time * 7) * 3;
        dc.PushTransform(new TranslateTransform(headX, headY));
        double headTilt = _groom * (12 + Math.Sin(_time * 5) * 5);
        if (CurrentInteraction == PetInteraction.Pet) headTilt += Math.Sin(_time * 3) * 3;
        if (CurrentInteraction == PetInteraction.Boop) headTilt -= 5;
        if (CurrentInteraction == PetInteraction.Play) headTilt += Math.Sin(_time * 4) * 3;
        dc.PushTransform(new RotateTransform(headTilt));
        dc.PushTransform(new ScaleTransform(1 - _sleep * .17, 1 - _sleep * .25));
        double earTwitch = Math.Sin(_time * 1.9) * 1.1;
        if (CurrentInteraction == PetInteraction.Pet) earTwitch += Math.Sin(_time * 9) * 2.4;
        dc.PushTransform(new RotateTransform(earTwitch, 0, -17));
        dc.DrawGeometry(Orange, Edge, Ears); dc.DrawGeometry(Pink, null, InnerEars);
        dc.Pop();
        dc.DrawEllipse(Orange, Edge, new Point(0, 0), 57, 51);
        dc.DrawEllipse(Light, null, new Point(-17, -12), 31, 29);
        dc.DrawEllipse(Cream, null, new Point(27, 21), 28, 21);
        dc.DrawEllipse(Blush, null, new Point(18, 17), 13, 7);
        bool closed = _sleep > .5 || _time % 5 < .17 || CurrentInteraction is PetInteraction.Pet or PetInteraction.Boop;
        Eye(dc, 17, -7, 10, closed); Eye(dc, 43, -11, 5.2, closed);
        if (CurrentInteraction is PetInteraction.Play or PetInteraction.Boop)
        {
            Curve(dc, new Point(8, -24), new Point(16, -29), new Point(25, -25), Stroke(Outline, 2));
            Curve(dc, new Point(39, -24), new Point(44, -28), new Point(48, -24), Stroke(Outline, 1.8));
        }
        dc.DrawEllipse(Pink, Fine, new Point(47, 12), 5, 3.7);
        dc.DrawLine(Fine, new Point(47, 16), new Point(45, 22));
        Curve(dc, new Point(45, 22), new Point(38, 28), new Point(33, 23), Fine);
        dc.DrawLine(Fine, new Point(29, 22), new Point(6, 19));
        dc.DrawLine(Fine, new Point(29, 27), new Point(5, 30));
        dc.DrawEllipse(Outline, null, new Point(32, 18), 1.2, 1.2);
        dc.DrawEllipse(Outline, null, new Point(36, 22), 1.1, 1.1);
        dc.DrawEllipse(Outline, null, new Point(29, 23), 1, 1);
        for (int i = 0; i < 3; i++)
            Curve(dc, new Point(-8 + i * 12, -47 + Math.Abs(1 - i) * 2),
                new Point(-10 + i * 12, -40), new Point(-6 + i * 12, -35), Stroke(Stripe, i == 1 ? 6 : 4.5));
        dc.Pop(); dc.Pop(); dc.Pop();
        if (_sleep > .7)
        {
            if (!FacingRight) dc.PushTransform(new ScaleTransform(-1, 1, 180, 180));
            dc.PushOpacity(.55 + .2 * Math.Sin(_time * 2));
            var z = new FormattedText("z z", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"), 21, Outline, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(z, new Point(FacingRight ? 125 : 195, 246 + Math.Sin(_time * 1.5) * 3)); dc.Pop();
            if (!FacingRight) dc.Pop();
        }
        if (Reminder)
        {
            dc.PushTransform(new TranslateTransform(294, 277 + Math.Sin(_time * 2) * 3));
            dc.DrawGeometry(Pink, null, Heart); dc.Pop();
        }
        DrawReaction(dc, headX, headY);
        if (!FacingRight) dc.Pop();
        dc.Pop(); dc.Pop();
    }
    private void DrawReaction(DrawingContext dc, double headX, double headY)
    {
        if (!IsInteracting) return;
        double t = _interaction.Elapsed;
        if (CurrentInteraction == PetInteraction.Pet)
            for (int i = 0; i < 3; i++)
            {
                double phase = (t * .5 + i * .3) % 1;
                dc.PushOpacity(1 - phase);
                dc.PushTransform(new TranslateTransform(headX - 30 + i * 26, headY - 85 - phase * 35));
                dc.PushTransform(new ScaleTransform(.55, .55)); dc.DrawGeometry(Pink, null, Heart);
                dc.Pop(); dc.Pop(); dc.Pop();
            }
        if (CurrentInteraction == PetInteraction.Boop)
        {
            double radius = 6 + (t % .6) * 9;
            var nose = new Point(headX + 47, headY + 12);
            dc.DrawEllipse(null, Stroke(Pink, 2.5), nose, radius, radius);
            for (int i = 0; i < 4; i++)
            {
                double a = i * Math.PI / 2;
                dc.DrawLine(Stroke(Pink, 2), nose + new Vector(Math.Cos(a), Math.Sin(a)) * (radius + 4),
                    nose + new Vector(Math.Cos(a), Math.Sin(a)) * (radius + 8));
            }
        }
        if (CurrentInteraction == PetInteraction.Treat)
        {
            dc.DrawEllipse(Cream, Fine, new Point(296, 331), 28, 9);
            dc.DrawRoundedRectangle(Pink, Edge, new Rect(270, 329, 52, 15), 7, 7);
            for (int i = 0; i < Math.Max(1, 6 - (int)t); i++)
                dc.DrawEllipse(Stripe, null, new Point(278 + i * 7, 326 + i % 2 * 3), 4, 3);
        }
        if (CurrentInteraction == PetInteraction.Play)
        {
            double x = 295 + Math.Sin(t * 3) * 15, y = 323 - Math.Abs(Math.Sin(t * 5)) * 11;
            Curve(dc, new Point(247, 337), new Point(266, 345), new Point(x, y), Stroke(Pink, 2.5));
            dc.DrawEllipse(Pink, Edge, new Point(x, y), 15, 15);
            Curve(dc, new Point(x - 11, y - 6), new Point(x + 6, y), new Point(x - 4, y + 12), Stroke(Cream, 2));
            Curve(dc, new Point(x + 4, y - 13), new Point(x - 7, y), new Point(x + 12, y + 5), Stroke(Cream, 2));
        }
        string text = CurrentInteraction switch { PetInteraction.Pet => "purrr...", PetInteraction.Boop => "mrrp!",
            PetInteraction.Treat => "nom nom", PetInteraction.Play => "play time!", PetInteraction.Wake => "big stretch", _ => "" };
        if (text.Length == 0) return;
        var caption = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"), 16, Outline, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        const double left = 113, top = 105;
        dc.DrawRoundedRectangle(Cream, Fine, new Rect(left, top, caption.Width + 22, 33), 14, 14);
        if (!FacingRight) dc.PushTransform(new ScaleTransform(-1, 1, 180, 180));
        dc.DrawText(caption, new Point(FacingRight ? left + 11 : 360 - left - 11 - caption.Width, top + 6));
        if (!FacingRight) dc.Pop();
    }
    private static void Paw(DrawingContext dc, double x, double y, double rx, double ry)
    {
        dc.DrawEllipse(Cream, Edge, new Point(x, y), rx, ry);
        dc.DrawLine(Stroke(Stripe, 1.5), new Point(x + 4, y + 3), new Point(x + 4, y + 7));
        dc.DrawLine(Stroke(Stripe, 1.5), new Point(x - 3, y + 4), new Point(x - 3, y + 8));
    }
    private static void Eye(DrawingContext dc, double x, double y, double radius, bool closed)
    {
        if (closed) Curve(dc, new Point(x - radius, y), new Point(x, y + 7), new Point(x + radius, y), Stroke(Eyes, 3));
        else
        {
            dc.DrawEllipse(Eyes, null, new Point(x, y), radius, radius * 1.3);
            dc.DrawEllipse(Brushes.White, null, new Point(x - radius * .3, y - radius * .4), radius * .32, radius * .38);
            dc.DrawEllipse(Cream, null, new Point(x + radius * .35, y + radius * .5), radius * .15, radius * .18);
        }
    }
    private static void Curve(DrawingContext dc, Point start, Point control, Point end, Pen pen)
    {
        var geometry = new StreamGeometry(); using (var path = geometry.Open()) { path.BeginFigure(start, false, false); path.QuadraticBezierTo(control, end, true, false); }
        dc.DrawGeometry(null, pen, geometry);
    }
    private static Brush ColorBrush(byte r, byte g, byte b) { var brush = new SolidColorBrush(Color.FromRgb(r, g, b)); brush.Freeze(); return brush; }
    private static Pen Stroke(Brush brush, double width) { var pen = new Pen(brush, width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round }; pen.Freeze(); return pen; }
}
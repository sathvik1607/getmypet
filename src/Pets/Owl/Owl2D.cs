using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Petcompanion;

// Original vector artwork with articulated wings, a tilting facial disc and continuous reactions.
internal sealed class Owl2D : FrameworkElement, IGroundPetVisual, IInteractivePetVisual
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly PetInteractionState _interaction = new();
    private double _time, _sleep, _groom, _eyeOpen = 1;
    private bool _facingRight = true;
    public bool FacingRight { get => _facingRight; set { _facingRight = value; InvalidateVisual(); } }
    public double Bank { get; set; }
    public double TravelHeadingDegrees { get; set; }
    public int PoseUpdates { get; private set; } = 1;
    public PetActivity Activity { get; set; } = PetActivity.Sitting;
    public bool Reminder { get; set; }
    public event Action<PetInteraction>? InteractionStarted;
    public bool IsInteracting => _interaction.Active;
    public PetInteraction CurrentInteraction => _interaction.Kind;
    private PetActivity EffectiveActivity => CurrentInteraction switch {
        PetInteraction.Nap => PetActivity.Sleeping, PetInteraction.None => Activity,
        PetInteraction.Treat => PetActivity.Grooming, _ => PetActivity.Sitting };

    private static readonly Brush Outline = Paint(90, 62, 63), Body = Paint(159, 111, 105),
        Head = Paint(180, 133, 119), Wings = Paint(132, 90, 91), WingLight = Paint(163, 117, 111),
        Cream = Paint(255, 235, 202), Face = Paint(255, 245, 220), Feather = Paint(203, 161, 139),
        Gold = Paint(237, 171, 79), Dark = Paint(61, 45, 52), Pink = Paint(232, 158, 150),
        Berry = Paint(163, 85, 117), Leaf = Paint(125, 154, 122);
    private static readonly Pen Edge = Stroke(Outline, 3.4), Fine = Stroke(Outline, 2);
    private static readonly Geometry Tufts = Shape("M -62,-30 Q -82,-54 -72,-72 Q -46,-69 -37,-49 Z M 37,-49 Q 46,-69 72,-72 Q 82,-54 62,-30 Z");
    private static readonly Geometry WingShape = Shape("M 0,-13 C -25,-7 -34,27 -13,56 Q -4,66 4,54 C 18,28 16,0 0,-13 Z");
    private static readonly Geometry Heart = Shape("M 0,4 C -13,-7 -21,10 0,25 C 21,10 13,-7 0,4 Z");
    private static readonly Geometry Beak = Shape("M -10,-4 Q 0,-12 10,-4 L 2,12 Q 0,15 -2,12 Z");

    public Owl2D()
    {
        IsHitTestVisible = false;
        _timer.Tick += (_, _) => { UpdatePose(.033); InvalidateVisual(); };
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
        IsVisibleChanged += (_, _) => { if (IsVisible) _timer.Start(); else _timer.Stop(); };
    }

    public void Interact(PetInteraction action) { _interaction.Trigger(action); InteractionStarted?.Invoke(action); InvalidateVisual(); }
    public void CancelInteraction() { _interaction.Cancel(); InvalidateVisual(); }
    public bool TryTap(Point point)
    {
        double x = point.X * 360, y = point.Y * 360;
        bool face = Math.Pow((x - 180) / 80, 2) + Math.Pow((y - 215 - _sleep * 12) / 69, 2) <= 1;
        bool belly = Math.Pow((x - 180) / 76, 2) + Math.Pow((y - 283) / 61, 2) <= 1;
        if (!face && !belly) return false;
        Interact(EffectiveActivity == PetActivity.Sleeping ? PetInteraction.Wake
            : Math.Abs(x - 180) < 15 && Math.Abs(y - 239 - _sleep * 12) < 15 ? PetInteraction.Boop : PetInteraction.Pet);
        return true;
    }

    internal void UpdatePose(double seconds)
    {
        seconds = Math.Max(0, seconds); _time += seconds; _interaction.Advance(seconds);
        _sleep += ((EffectiveActivity == PetActivity.Sleeping ? 1 : 0) - _sleep) * .13;
        _groom += ((EffectiveActivity == PetActivity.Grooming ? 1 : 0) - _groom) * .13;
        bool closed = _sleep > .55 || _time % 4.8 < .15 || CurrentInteraction is PetInteraction.Pet or PetInteraction.Boop;
        _eyeOpen += ((closed ? .07 : 1) - _eyeOpen) * .38;
        PoseUpdates++;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double scale = Math.Min(ActualWidth, ActualHeight) / 360;
        if (scale <= 0) return;
        dc.PushTransform(new TranslateTransform((ActualWidth - 360 * scale) / 2, (ActualHeight - 360 * scale) / 2));
        dc.PushTransform(new ScaleTransform(scale, scale));
        bool walking = EffectiveActivity == PetActivity.Walking;
        double hop = walking ? Math.Abs(Math.Sin(_time * 5.5)) * 7 : 0;
        if (CurrentInteraction == PetInteraction.Play) hop += Math.Max(0, Math.Sin(_time * 6)) * 8;
        double breathe = Math.Sin(_time * 1.7) * 1.2;
        dc.PushOpacity(.10); dc.DrawEllipse(Outline, null, new Point(180, 345), 79 - hop * .8, 5); dc.Pop();
        // Feet remain visible on either side of the fluffy belly.
        Foot(dc, 154, 338 - hop, walking ? Math.Sin(_time * 5.5) * 3 : 0);
        Foot(dc, 206, 338 - hop, walking ? -Math.Sin(_time * 5.5) * 3 : 0);
        dc.PushTransform(new TranslateTransform(0, -hop));
        double bodyY = 281 + breathe + _sleep * 5;
        dc.DrawEllipse(Body, Edge, new Point(180, bodyY), 75 + _sleep * 3, 62 - _sleep * 4);
        dc.DrawEllipse(Cream, null, new Point(180, bodyY + 12), 51, 45);
        // Scalloped feather marks and subtle breast highlights.
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
            {
                double x = 154 + col * 25 + (row % 2 == 0 ? 0 : 3), y = bodyY - 2 + row * 16;
                Curve(dc, new Point(x - 5, y), new Point(x, y + 7), new Point(x + 5, y), Stroke(Feather, 2.2));
            }
        double flap = walking ? Math.Sin(_time * 5.5) * 10 : Math.Sin(_time * 1.5) * 2;
        if (CurrentInteraction == PetInteraction.Play) flap = Math.Sin(_time * 7) * 30 - 12;
        if (CurrentInteraction == PetInteraction.Wake) flap = -34 - Math.Sin(_time * 2) * 14;
        double tuck = _sleep * 28 + _groom * 18;
        Wing(dc, 112, bodyY - 10, -8 - flap + tuck, false);
        Wing(dc, 248, bodyY - 10, 8 + flap - tuck - (Reminder ? 24 + Math.Sin(_time * 2) * 7 : 0), true);
        double tilt = (FacingRight ? 1 : -1) * 3 + Math.Sin(_time * .9) * 3;
        tilt += _groom * Math.Sin(_time * 3) * 13;
        if (CurrentInteraction == PetInteraction.Boop) tilt += Math.Sin(_interaction.Elapsed * 6) * 13;
        dc.PushTransform(new TranslateTransform(180, 215 + breathe + _sleep * 12));
        dc.PushTransform(new RotateTransform(tilt));
        dc.DrawGeometry(Head, Edge, Tufts);
        dc.DrawEllipse(Head, Edge, new Point(0, 0), 79, 67);
        // Two overlapping facial discs create an owl's distinct heart-shaped mask.
        dc.DrawEllipse(Cream, null, new Point(-30, 1), 40, 47);
        dc.DrawEllipse(Cream, null, new Point(30, 1), 40, 47);
        dc.DrawEllipse(Face, null, new Point(-30, -3), 34, 37);
        dc.DrawEllipse(Face, null, new Point(30, -3), 34, 37);
        Eye(dc, -30, -2, _eyeOpen, FacingRight ? 3 : -3);
        Eye(dc, 30, -2, _eyeOpen, FacingRight ? 3 : -3);
        dc.DrawEllipse(Pink, null, new Point(-51, 27), 11, 5);
        dc.DrawEllipse(Pink, null, new Point(51, 27), 11, 5);
        dc.PushTransform(new TranslateTransform(0, 24)); dc.DrawGeometry(Gold, Fine, Beak); dc.Pop();
        for (int i = -1; i <= 1; i++)
            dc.DrawEllipse(Feather, null, new Point(i * 14, -52 + Math.Abs(i) * 2), 3, 6);
        dc.Pop(); dc.Pop();
        dc.Pop();
        if (_sleep > .65) Caption(dc, "z z", 236, 163 + Math.Sin(_time * 1.5) * 3, false);
        DrawReaction(dc);
        dc.Pop(); dc.Pop();
    }

    private void DrawReaction(DrawingContext dc)
    {
        double t = _interaction.Elapsed;
        if (CurrentInteraction == PetInteraction.Pet)
            for (int i = 0; i < 3; i++)
            {
                double phase = (t * .55 + i * .3) % 1;
                dc.PushOpacity(1 - phase);
                dc.PushTransform(new TranslateTransform(147 + i * 27, 134 - phase * 31));
                dc.PushTransform(new ScaleTransform(.5, .5)); dc.DrawGeometry(Pink, null, Heart);
                dc.Pop(); dc.Pop(); dc.Pop();
            }
        if (CurrentInteraction == PetInteraction.Boop)
            dc.DrawEllipse(null, Stroke(Gold, 2), new Point(180, 241), 12 + (t % .6) * 12, 12 + (t % .6) * 12);
        if (CurrentInteraction == PetInteraction.Treat)
        {
            dc.DrawEllipse(Cream, Fine, new Point(285, 335), 26, 7);
            dc.DrawRoundedRectangle(Feather, Fine, new Rect(261, 331, 48, 12), 5, 5);
            for (int i = 0; i < Math.Max(1, 5 - (int)t); i++)
            {
                dc.DrawEllipse(Berry, null, new Point(269 + i * 9, 328 + i % 2 * 2), 4.8, 5.4);
                dc.DrawEllipse(Leaf, null, new Point(269 + i * 9, 322 + i % 2 * 2), 3.8, 1.8);
            }
        }
        if (CurrentInteraction == PetInteraction.Play)
        {
            double x = 282 + Math.Sin(t * 3) * 18, y = 194 + Math.Sin(t * 4) * 16;
            dc.PushOpacity(.18); dc.DrawEllipse(Gold, null, new Point(x, y), 17, 17); dc.Pop();
            dc.DrawEllipse(Gold, Fine, new Point(x, y), 7, 7);
            dc.DrawEllipse(Face, null, new Point(x - 2, y - 2), 2, 2);
            dc.DrawEllipse(Face, null, new Point(x - 7, y - 5), 4, 2);
            dc.DrawEllipse(Face, null, new Point(x + 7, y - 5), 4, 2);
        }
        if (Reminder)
        {
            dc.PushTransform(new TranslateTransform(282, 272 + Math.Sin(_time * 2) * 3));
            dc.DrawGeometry(Pink, null, Heart); dc.Pop();
        }
        string text = CurrentInteraction switch { PetInteraction.Pet => "hoo, lovely!", PetInteraction.Boop => "who? me?",
            PetInteraction.Treat => "berry nice", PetInteraction.Play => "twinkle chase", PetInteraction.Wake => "hello, world", _ => "" };
        if (text.Length > 0) Caption(dc, text, 105, 91, true);
    }
    private static void Foot(DrawingContext dc, double x, double y, double step)
    {
        dc.DrawEllipse(Gold, Fine, new Point(x, y - Math.Max(0, step)), 13, 7);
        for (int i = -1; i <= 1; i++) dc.DrawLine(Fine, new Point(x + i * 5, y + 1 - Math.Max(0, step)), new Point(x + i * 5, y + 5 - Math.Max(0, step)));
    }
    private static void Wing(DrawingContext dc, double x, double y, double angle, bool mirror)
    {
        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new RotateTransform(angle));
        if (mirror) dc.PushTransform(new ScaleTransform(-1, 1));
        dc.DrawGeometry(Wings, Edge, WingShape);
        Curve(dc, new Point(-9, 8), new Point(-18, 29), new Point(-7, 45), Stroke(WingLight, 3));
        Curve(dc, new Point(0, 13), new Point(-6, 32), new Point(0, 42), Stroke(WingLight, 2.5));
        if (mirror) dc.Pop(); dc.Pop(); dc.Pop();
    }
    private static void Eye(DrawingContext dc, double x, double y, double open, double gaze)
    {
        if (open < .25)
        {
            Curve(dc, new Point(x - 18, y - 1), new Point(x, y + 13), new Point(x + 18, y - 1), Stroke(Outline, 3.3));
            return;
        }
        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new ScaleTransform(1, open));
        dc.DrawEllipse(Gold, Stroke(Outline, 2), default, 23, 25);
        dc.DrawEllipse(Dark, null, new Point(gaze, 1), 17.5, 20);
        dc.DrawEllipse(Brushes.White, null, new Point(gaze - 6, -7), 6.5, 8);
        dc.DrawEllipse(Cream, null, new Point(gaze + 7, 9), 3, 3.8);
        dc.Pop(); dc.Pop();
    }
    private void Caption(DrawingContext dc, string text, double x, double y, bool bubble)
    {
        var caption = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"), bubble ? 15 : 19, Outline, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        if (bubble) dc.DrawRoundedRectangle(Face, Fine, new Rect(x, y, caption.Width + 22, 32), 14, 14);
        dc.DrawText(caption, new Point(x + (bubble ? 11 : 0), y + (bubble ? 6 : 0)));
    }
    private static void Curve(DrawingContext dc, Point start, Point control, Point end, Pen pen)
    {
        var geometry = new StreamGeometry(); using (var p = geometry.Open()) { p.BeginFigure(start, false, false); p.QuadraticBezierTo(control, end, true, false); }
        dc.DrawGeometry(null, pen, geometry);
    }
    private static Brush Paint(byte r, byte g, byte b) { var brush = new SolidColorBrush(Color.FromRgb(r, g, b)); brush.Freeze(); return brush; }
    private static Pen Stroke(Brush brush, double width) { var pen = new Pen(brush, width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round }; pen.Freeze(); return pen; }
    private static Geometry Shape(string path) { var shape = Geometry.Parse(path); shape.Freeze(); return shape; }
}
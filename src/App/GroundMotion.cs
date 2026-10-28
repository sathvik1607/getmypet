using System;
using System.Windows;

namespace Petcompanion;

internal readonly record struct GroundStep(Point Position, PetActivity Activity);
internal sealed class GroundMotion
{
    private double _time, _x, _target;
    private Rect _bounds;
    private bool _ready, _right;
    public void Reset() => _ready = false;
    public static PetActivity ActivityAt(double seconds) => (seconds % 32) switch
    { < 9 => PetActivity.Walking, < 13 => PetActivity.Sitting, < 19 => PetActivity.Grooming, _ => PetActivity.Sleeping };

    public GroundStep Advance(Point actual, Rect bounds, double seconds, Movement mode, Point cursor)
    {
        if (!_ready || bounds != _bounds || Math.Abs(actual.X - _x) > 3)
        { _bounds = bounds; _x = Math.Clamp(actual.X, bounds.Left, bounds.Right); _target = bounds.Left + bounds.Width * (_x < bounds.Left + bounds.Width / 2 ? .85 : .15); _time = 0; _ready = true; }
        seconds = Math.Clamp(seconds, 0, .1); _time += seconds;
        var activity = ActivityAt(_time);
        if (mode == Movement.Stay) return new(actual, activity == PetActivity.Walking ? PetActivity.Sitting : activity);
        if (mode == Movement.Follow)
        {
            _target = Math.Clamp(cursor.X + (cursor.X > _x ? -75 : 75), bounds.Left, bounds.Right);
            activity = Math.Abs(_target - _x) > 5 ? PetActivity.Walking : PetActivity.Sitting;
        }
        if (activity == PetActivity.Walking)
        {
            if (mode == Movement.Wander && Math.Abs(_target - _x) < 1)
            { _right = !_right; _target = bounds.Left + bounds.Width * (_right ? .85 : .15); }
            _x += Math.Sign(_target - _x) * Math.Min(Math.Abs(_target - _x), (mode == Movement.Follow ? 160 : 85) * seconds);
        }
        return new(new Point(_x, bounds.Bottom), activity);
    }
}
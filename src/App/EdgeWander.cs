using System;
using System.Windows;

namespace Petcompanion;

// Prefer an outer band rather than the exact border. All positions are window top-left coordinates.
internal sealed class EdgeWander
{
    private readonly Random _random;
    private Rect _bounds;
    private Point _position, _target;
    private bool _ready, _crossing;
    private int _direction = 1, _sector;
    private double _activeSeconds;
    public EdgeWander(int? seed = null) => _random = seed is int value ? new Random(value) : new Random();
    public void Reset() => _ready = false;

    public Point Advance(Point actual, Rect bounds, double seconds)
    {
        if (!_ready || bounds != _bounds || (actual - _position).Length > 3)
        {
            _bounds = bounds;
            _position = new Point(Math.Clamp(actual.X, bounds.Left, bounds.Right), Math.Clamp(actual.Y, bounds.Top, bounds.Bottom));
            double x = bounds.Width > 0 ? (_position.X - bounds.Left) / bounds.Width - .5 : 0;
            double y = bounds.Height > 0 ? (_position.Y - bounds.Top) / bounds.Height - .5 : 0;
            _sector = ((int)Math.Round(Math.Atan2(y, x) * 6 / Math.PI) + 12) % 12;
            _target = BandPoint(_sector);
            _crossing = false; _activeSeconds = 0;
            _direction = _random.Next(2) == 0 ? -1 : 1;
            _ready = true;
        }
        seconds = Math.Clamp(seconds, 0, .1);
        if (seconds == 0) return _position;
        _activeSeconds += seconds;
        if ((_target - _position).Length < .5)
        {
            _crossing = _activeSeconds >= 90 && bounds.Width > 0 && bounds.Height > 0;
            if (_crossing) { _sector = (_sector + 6) % 12; _activeSeconds = 0; }
            else
            {
                if (_random.Next(12) == 0) _direction = -_direction;
                _sector = (_sector + _direction * (_random.Next(4) == 0 ? 2 : 1) + 12) % 12;
            }
            _target = BandPoint(_sector);
        }
        var delta = _target - _position;
        double distance = delta.Length;
        if (distance > 0) _position += delta * (Math.Min(distance, (_crossing ? 320 : 125) * seconds) / distance);
        return _position;
    }

    private Point BandPoint(int sector)
    {
        // Four to twenty-two percent inset, with jitter so successive circuits differ.
        double angle = sector * Math.PI / 6 + (_random.NextDouble() - .5) * .22;
        double x = Math.Cos(angle), y = Math.Sin(angle);
        double radius = (.5 - (.04 + _random.NextDouble() * .18)) / Math.Max(Math.Abs(x), Math.Abs(y));
        return new Point(_bounds.Left + (.5 + x * radius) * _bounds.Width,
            _bounds.Top + (.5 + y * radius) * _bounds.Height);
    }
}
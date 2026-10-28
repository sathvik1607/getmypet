using System;
using System.Windows;

namespace Petcompanion;

internal static class WanderTests
{
    public static void Run(Action<bool, string> check)
    {
        check(PetHeading.FromVelocity(0, -1) == -90 && PetHeading.FromVelocity(0, 1) == 90, "Vertical heading follows travel");
        check(PetHeading.FromVelocity(1, 0) == 0 && PetHeading.FromVelocity(-1, 0) == 180, "Horizontal heading follows travel");
        check(PetHeading.FromVelocity(1, 1) == 45 && PetHeading.Relative(180, false) == 0, "Diagonal heading and left-facing compensation");
        check(PetHeading.Delta(350, 10) == 20 && PetHeading.Delta(10, 350) == -20, "Shortest turn across angle wrap");
        foreach (var bounds in new[] { new Rect(0, 0, 1540, 810), new Rect(-1600, -200, 900, 700), new Rect(0, 0, 0, 400), new Rect(0, 0, 0, 0) })
        {
            var route = new EdgeWander(42);
            var position = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
            int central = 0, outerBand = 0, interior = 0;
            bool stayedInside = true, speedLimited = true;
            for (int i = 0; i < 10000; i++)
            {
                var next = route.Advance(position, bounds, .1);
                stayedInside &= bounds.Contains(next);
                speedLimited &= (next - position).Length <= 32.01;
                position = next;
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    double x = (position.X - bounds.Left) / bounds.Width, y = (position.Y - bounds.Top) / bounds.Height;
                    if (x > .3 && x < .7 && y > .3 && y < .7) central++;
                                        if (x < .3 || x > .7 || y < .3 || y > .7) outerBand++;
                    if (x > .08 && x < .92 && y > .08 && y < .92) interior++;
                }
            }
            check(stayedInside && speedLimited, "Wander bounds and speed: " + bounds);
            if (bounds.Width > 0 && bounds.Height > 0)
                            {
                check(central < 1200 && outerBand > 8800, "Centre occupancy below 12%, outer band above 88%: " + bounds);
                check(interior > 2000, "Meaningful travel away from the border: " + bounds);
            }
            check(route.Advance(position, bounds, 0) == position, "Paused route does not advance");
            var centre = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
            route.Reset();
            var away = route.Advance(centre, bounds, .1);
            check(bounds.Width == 0 && bounds.Height == 0 || (away - centre).Length > 0, "Leaves centre after repositioning");
        }
    }
}
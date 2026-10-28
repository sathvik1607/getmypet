using System;

namespace Petcompanion;

internal static class PetHeading
{
    // Screen Y increases downward, so positive angles point clockwise.
    public static double FromVelocity(double dx, double dy) => Math.Atan2(dy, dx) * 180 / Math.PI;
    public static double Delta(double from, double to) => ((to - from) % 360 + 540) % 360 - 180;
    public static double Relative(double heading, bool facingRight) => Delta(facingRight ? 0 : 180, heading);
}
using System;
using System.Text.Json;
using System.Windows;

namespace Petcompanion;

internal static class CatTests
{
    public static void Run(Action<bool, string> check)
    {
        check(JsonSerializer.Deserialize<Settings>("{}")!.Validate().PetId == "dragon", "Legacy settings retain dragon");
        check(new Settings { PetId = "missing" }.Validate().PetId == "dragon", "Unknown pet falls back safely");
        check(JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(new Settings { PetId = "cat" }))!.Validate().PetId == "cat", "Cat selection persists");
        check(PetCatalog.Find("cat").Locomotion == PetLocomotion.Ground && PetCatalog.Find("cat").IntroAudio?.EndsWith("mew.wav", StringComparison.Ordinal) == true, "Cat is grounded and has its own mew");
        check(GroundMotion.ActivityAt(1) == PetActivity.Walking && GroundMotion.ActivityAt(10) == PetActivity.Sitting &&
            GroundMotion.ActivityAt(14) == PetActivity.Grooming && GroundMotion.ActivityAt(20) == PetActivity.Sleeping &&
            GroundMotion.ActivityAt(33) == PetActivity.Walking, "Cat activity cycle");
        check(PetCatalog.Find("cat").CreatePresentation(370, false).View is Cat2D &&
            PetCatalog.Find("cat-3d").CreatePresentation(370, false).View is CatModel, "2D default and preserved 3D comparison");
        foreach (var bounds in new[] { new Rect(0, 0, 900, 400), new Rect(-1200, -200, 800, 700), new Rect(0, 0, 0, 0) })
        {
            var ground = new GroundMotion(); var position = new Point(bounds.Left, bounds.Bottom);
            bool contained = true, resting = true, walked = false;
            for (int i = 0; i < 640; i++)
            {
                var next = ground.Advance(position, bounds, .1, Movement.Wander, default);
                contained &= bounds.Contains(next.Position) && next.Position.Y == bounds.Bottom;
                if (next.Activity != PetActivity.Walking) resting &= position == next.Position;
                else walked |= next.Position != position;
                position = next.Position;
            }
            check(contained && resting && (bounds.Width == 0 || walked), "Grounded movement and stationary resting: " + bounds);
            var stay = ground.Advance(position, bounds, .1, Movement.Stay, default);
            check(stay.Position == position && stay.Activity != PetActivity.Walking, "Stay never walks in place");
            var follow = ground.Advance(position, bounds, .1, Movement.Follow, new Point(bounds.Right, bounds.Top));
            check(follow.Position.Y == bounds.Bottom && bounds.Contains(follow.Position), "Follow stays grounded");
        }
    }
}
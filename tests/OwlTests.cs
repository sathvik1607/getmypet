using System;
using System.Windows;

namespace Petcompanion;

internal static class OwlTests
{
    public static void Run(Action<bool, string> check)
    {
        var pet = new OwlPet();
        check(pet.Id == "owl" && pet.Locomotion == PetLocomotion.Ground && pet.IntroAudio?.EndsWith("hoot.wav", StringComparison.Ordinal) == true && pet.ReminderAudio(BreakKind.Water) == null,
            "Owl is a grounded companion with a soft hoot");
        var desktop = pet.CreatePresentation(370, false); var reminder = pet.CreatePresentation(500, true);
        check(desktop.View is Owl2D && reminder.View is Owl2D && !ReferenceEquals(desktop.View, reminder.View), "Independent owl host and reminder views");
        var owl = (Owl2D)desktop.Visual;
        check(!owl.TryTap(new Point(.05, .05)) && !owl.IsInteracting, "Owl ignores empty-space taps");
        check(owl.TryTap(new Point(.5, 239d / 360)) && owl.CurrentInteraction == PetInteraction.Boop, "Owl beak tap boops");
        owl.CancelInteraction(); owl.FacingRight = false;
        check(owl.TryTap(new Point(.5, 239d / 360)) && owl.CurrentInteraction == PetInteraction.Boop, "Owl beak tap survives gaze direction change");
        owl.Interact(PetInteraction.Nap); owl.UpdatePose(180);
        check(owl.CurrentInteraction == PetInteraction.Nap, "Owl requested nap persists");
        check(owl.TryTap(new Point(.5, .76)) && owl.CurrentInteraction == PetInteraction.Wake, "Owl tap wakes requested nap");
        owl.UpdatePose(3); check(!owl.IsInteracting, "Owl wake returns to ordinary activity");
        owl.Interact(PetInteraction.Play); owl.UpdatePose(9);
        check(!owl.IsInteracting && owl.PoseUpdates > 2, "Owl play completes with live pose updates");
        owl.Interact(PetInteraction.Pet); owl.CancelInteraction(); check(!owl.IsInteracting, "Owl dragging can cancel a reaction");
    }
}
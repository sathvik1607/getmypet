using System;
using System.Windows;

namespace Petcompanion;

internal static class InteractionTests
{
    public static void Run(Action<bool, string> check)
    {
        var state = new PetInteractionState();
        state.Trigger(PetInteraction.Treat); state.Advance(2);
        check(state.Kind == PetInteraction.Treat && state.Remaining == 4, "Treat reaction duration");
        state.Trigger(PetInteraction.Play);
        check(state.Kind == PetInteraction.Play && state.Elapsed == 0, "New action replaces old reaction");
        state.Advance(8); check(!state.Active, "Transient reaction ends");
        state.Trigger(PetInteraction.Nap); state.Advance(3600);
        check(state.Kind == PetInteraction.Nap, "Requested nap lasts until interrupted");
        state.Trigger(PetInteraction.Wake); state.Advance(2);
        check(!state.Active, "Wake returns to ordinary behavior");
        state.Trigger(PetInteraction.Pet); state.Cancel(); check(!state.Active, "Drag cancels reaction");
        var cat = new Cat2D();
        int soundEvents = 0; cat.InteractionStarted += _ => soundEvents++;
        check(!cat.TryTap(new Point(.1, .1)) && !cat.IsInteracting, "Blank space does not pet the cat");
        check(cat.TryTap(new Point(289d / 360, 247d / 360)) && cat.CurrentInteraction == PetInteraction.Boop, "Nose tap boops");
        cat.FacingRight = false; cat.CancelInteraction();
        check(cat.TryTap(new Point((360d - 289) / 360, 247d / 360)) && cat.CurrentInteraction == PetInteraction.Boop, "Mirrored nose tap boops");
        cat.FacingRight = true; cat.CancelInteraction();
        check(cat.TryTap(new Point(160d / 360, 290d / 360)) && cat.CurrentInteraction == PetInteraction.Pet, "Body tap pets");
        check(cat.TryTap(new Point(207d / 360, 175d / 360)) && cat.CurrentInteraction == PetInteraction.Pet, "Ear tap scratches rather than boops");
        cat.Interact(PetInteraction.Nap);
        check(cat.TryTap(new Point(160d / 360, 310d / 360)) && cat.CurrentInteraction == PetInteraction.Wake, "Tap wakes requested nap");
        check(soundEvents >= 5, "Taps and actions emit pet audio events");
        cat.Interact(PetInteraction.Play); cat.UpdatePose(9);
        check(!cat.IsInteracting && cat.Activity == PetActivity.Walking, "Reaction leaves normal activity intact");
    }
}
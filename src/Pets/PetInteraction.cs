using System;
using System.Windows;

namespace Petcompanion;

internal enum PetInteraction { None, Pet, Boop, Treat, Play, Nap, Wake }
internal interface IInteractivePetVisual : IPetVisual
{
    event Action<PetInteraction>? InteractionStarted;
    bool IsInteracting { get; }
    PetInteraction CurrentInteraction { get; }
    void Interact(PetInteraction action);
    bool TryTap(Point normalizedPoint);
    void CancelInteraction();
}

// Transient reactions never change reminder deadlines or saved movement preferences.
internal sealed class PetInteractionState
{
    public PetInteraction Kind { get; private set; }
    public double Remaining { get; private set; }
    public double Elapsed { get; private set; }
    public bool Active => Kind != PetInteraction.None;
    public void Trigger(PetInteraction action)
    {
        Kind = action; Elapsed = 0;
        Remaining = action switch { PetInteraction.None => 0, PetInteraction.Pet => 4,
            PetInteraction.Boop => 2, PetInteraction.Treat => 6, PetInteraction.Play => 8,
            PetInteraction.Nap => double.PositiveInfinity, PetInteraction.Wake => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(action)) };
    }
    public void Advance(double seconds)
    {
        if (!Active || seconds <= 0) return;
        Elapsed += seconds;
        if (!double.IsPositiveInfinity(Remaining)) Remaining = Math.Max(0, Remaining - seconds);
        if (Remaining <= 0) Kind = PetInteraction.None;
    }
    public void Cancel() => Trigger(PetInteraction.None);
}
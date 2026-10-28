using System.Windows;

namespace Petcompanion;

// Each instance owns its view and animation state. The shared host controls position and direction.
internal interface IPetVisual
{
    bool FacingRight { get; set; }
    double Bank { get; set; }
    double TravelHeadingDegrees { get; set; }
    int PoseUpdates { get; }
}

internal sealed record PetPresentation(FrameworkElement View, IPetVisual Visual);

internal enum PetLocomotion { Flying, Ground }
internal enum PetActivity { Walking, Sitting, Grooming, Sleeping }
internal interface IGroundPetVisual : IPetVisual { PetActivity Activity { get; set; } }

internal interface IPetDefinition
{
    string Id { get; }
    string DisplayName { get; }
    PetLocomotion Locomotion => PetLocomotion.Flying;
    string? IntroAudio { get; }
    string? ReminderAudio(BreakKind kind);
    string? InteractionAudio(PetInteraction action) => null;
    PetPresentation CreatePresentation(double width, bool reminder);
}
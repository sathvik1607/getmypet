namespace Petcompanion;

internal sealed class OwlPet : IPetDefinition
{
    public string Id => "owl";
    public string DisplayName => "Moon owl";
    public PetLocomotion Locomotion => PetLocomotion.Ground;
    public string? IntroAudio => "Pets/Owl/Assets/Audio/hoot.wav";
    public string? InteractionAudio(PetInteraction action) => action is PetInteraction.None or PetInteraction.Nap ? null : IntroAudio;
    public string? ReminderAudio(BreakKind kind) => null;
    public PetPresentation CreatePresentation(double width, bool reminder)
    {
        var owl = new Owl2D { Width = width, Height = width, Reminder = reminder,
            Activity = reminder ? PetActivity.Sitting : PetActivity.Walking };
        return new(owl, owl);
    }
}
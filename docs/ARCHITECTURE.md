# Project layout and adding pets

```text
src/
  App/                 Entry point, desktop lifecycle and controller
  Configuration/       Saved preferences; existing LocalAppData location retained
  Reminders/           Reminder records and scheduler (no renderer dependency)
  UI/                  Shared controls, settings, pet host and reminder windows
  Pets/
    IPetDefinition.cs  Pet visuals, presentation and optional audio contract
    PetCatalog.cs      Available definitions and current default
    Cat/               CatPet.cs, Cat2D.cs and comparison CatModel.cs
    Owl/               OwlPet.cs and animated Owl2D.cs
    Dragon/
      DragonPet.cs     Presentation, flame effects and audio mapping
      DragonModel.cs   Dragon mesh and animation
      Assets/Audio/   Runtime WAV files
      Assets/References/  Concept art, excluded from runtime output
      Exports/        Static GLB, screenshot and validator result
Assets/Brand/          Shared app logo
tests/                 Self-checks and muted audio checks
tools/                 Dragon model viewer and generic GLB exporter
scripts/               Build, install, audio generation and profiling
```

The root project automatically includes C# files in these folders. Namespaces and executable identity remain `Petcompanion`; folder placement separates ownership without changing saved settings or launcher commands. Test and tool entry modes remain in the same assembly.

`PetController` and `PetWindow` consume `IPetDefinition` and `IPetVisual`, not the dragon model. Each presentation creates an independent WPF view and animation state. The host supplies movement direction, screen heading and banking; the pet owns appearance and effects. Reminder scheduling and custom text remain shared. Compact popups do not create pet models.

To add another pet:

1. Add `src/Pets/Cat/` (or `Dog/`) with an `IPetDefinition` implementation and a visual implementing `IPetVisual`. Create a new view on every `CreatePresentation` call; a WPF element cannot have two parents. Stop animation work when the view is hidden/unloaded, as the dragon does.
2. Keep that pet's audio under `Assets/Audio/`, concept art under `Assets/References/`, and deliberate exports under `Exports/` within its folder. Runtime audio is automatically copied to `Pets/<name>/Assets/Audio/`; return paths relative to the executable directory. Audio properties may return null for silent pets.
3. Register the definition in `PetCatalog.All`. Its stable ID appears in the existing selector and persists through `Settings.PetId`; unknown IDs fall back to the dragon. Set `Locomotion` to Ground for a pet that walks rather than flies.
4. Keep pet-specific effects in that implementation. Shared UI must use `CreatePresentation`; it should not construct a concrete model. The dragon development viewer may access its concrete model for camera and mesh export.
5. Run `petcompanion.cmd test`, and verify the new pet in its host and full-screen presentation. Preserve popup focus behavior, snooze, quiet hours, lock/suspend handling and audio preferences.

Dragon, Orange cat and Moon owl are available in the pet selector. Dog assets, renderer plugin loading and an animated GLB importer are not included. Dragon exports remain static poses.
`src/App/EdgeWander.cs` owns varied outer-band routing independently of WPF windows. Its clock advances only during active wandering; manual repositioning and working-area changes replan toward the outer band. `tests/WanderTests.cs` simulates routes on normal, negative-coordinate and degenerate working areas, checking containment, speed, pause behavior and centre occupancy.

The desktop host keeps a square rotation area. Screen heading uses rotation only; it must not introduce angle-dependent scale changes. Keep geometry within that fixed area and check transparent edge clearance in rendered previews.

Grounded pets use `src/App/GroundMotion.cs` and `IGroundPetVisual.Activity`. The cat cycles through walking (9 seconds), sitting (4), grooming (6), and sleeping (13). Wander travel stays along the bottom of the working area; Follow follows the pointer horizontally along that bottom; Stay holds its location and never walks in place. The shared host swaps presentations in place when settings are saved; unloaded models stop their animation timers. Cat reminders use the same scheduling/dismissal UI and gentle synthesized pet cues. Tests cover migration, saved pet IDs, grounded/resting movement, switching through the real selector, and full-screen/popup reminders.

`Cat2D.cs` is the primary cat renderer (stable ID `cat`). It draws vector shapes and continuous poses with left/right mirroring. The original `CatModel.cs` stays registered as `cat-3d` for comparison. Ground motion and reminder logic consume the same interface for both.

`IInteractivePetVisual` advertises optional interaction support. `PetInteractionState` owns transient reaction timing; Cat2D supplies hit regions, gestures and drawings. The host distinguishes taps from drags and converts taps to normalized presentation coordinates. The controller freezes route advancement while a reaction is active; reminders use a separate clock. Interaction state is not persisted and is discarded on pet replacement. Nap is indefinite until Wake, another action, or drag. The 3D comparison does not advertise this capability.

## Work mode and owl

`OwlPet` registers the stable ID `owl`. `Owl2D` implements the existing ground/activity/interaction contracts; no controller renderer branch is needed. `FocusSession` owns an in-memory countdown, independent of preferences. `PetController` parks/rests the pet, substitutes popup style only for automatically scheduled reminders, and restores normal movement when focus ends. Completion notifications have no reminder identity, do not reschedule reminders, and do not offer a nonfunctional Snooze. Session lock and power suspension are separate conditions so a power resume cannot unpause a still-locked session.

`Settings.ClickThrough` persists the user's choice. `PetWindow.SetClickThrough` toggles the transparent layered-window style and leaves the tray accessible; the tray can restore interaction without resetting reminder deadlines. UI smoke checks verify the native style as well as the recovery path. Dashboard live previews own separate models, unload old previews on selection changes, and detach their status timer on close.

Pet sound selection belongs to `IPetDefinition.IntroAudio`/`InteractionAudio`. Interactive visuals emit `InteractionStarted` for taps and menu actions alike; the controller subscribes/unsubscribes on pet replacement, applies mute/volume, and throttles rapid cue playback. Pet cues use a separate player from reminder speech. `scripts/Make-Pet-Audio.ps1` deterministically regenerates the original synthesized cat/owl WAV files. UI smoke windows do not activate or force themselves above ongoing work; full-screen reminder dimensions and dismissal code still run in that test mode.

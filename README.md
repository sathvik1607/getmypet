# Petcompanion

An offline Windows desktop companion with a flying dragon, a sleepy orange cat, and a curious Moon owl. A live 3D dragon bends its spine continuously, paddles its legs, and sweeps its tail. It wanders through varied outer-screen positions with brief crossings, turns to face its direction of travel, or follows your pointer. Reminders can use a compact popup or a full-screen presentation of your selected pet. Full-screen dragon reminders include flame, roar and voice; cat reminders use a gentle mew.

## Use

Open **Petcompanion** from the Desktop or Start menu. Settings open alongside the pet. Closing settings leaves the pet running.

- Choose **Dragon**, **Orange cat**, or **Moon owl** under **Your companion** in Settings and click **Save** to switch immediately. Your choice persists across restarts.
- Drag the pet to place it. Right-click it or its tray icon for settings, movement modes, pause, preview, mute, and quit. Double-click the pet for settings.
- **Wander** flies between varied positions in the outer areas of its current monitor. **Follow** moves toward your pointer, with a gap. **Stay** keeps the pet in place while its body keeps moving.
- Water defaults to every 45 minutes and movement to every 60 minutes. These are editable preferences, not medical recommendations. Save restarts the timers.
- Reminders cover the pet's current monitor. Other monitors remain usable. **Done**, **Skip**, **Esc**, or Alt+F4 dismiss; **Snooze 5 min** delays that reminder. The countdown dismisses automatically.
- Default volume is 40%. **Test water** and **Test stretch** play a reminder immediately, even during quiet hours. Turn your speaker volume down before your first preview.
- Quiet hours are enabled by default: 10 pm–8 am, using Windows local time. Scheduled reminders pause during quiet hours, session lock, and suspend; timers restart afterward. You can turn quiet hours off.
- Quit from the pet/tray menu. Automatic startup is off. To enable it manually, copy the Desktop shortcut into the folder opened by `shell:startup`.

The pet is a stylized procedural 3D model rendered with Windows WPF: a 64-sample curved spine, deforming tube, articulated legs, horns, whiskers, eyes, and head. Geometry updates about 30 times per second; no image swapping is used. This simple model is not an exact or photorealistic Shenron recreation. There is no lip sync. Its voice is Windows speech lowered in pitch, not the original character's actor. The roar is a locally synthesized bass growl.

## Build, test, install

Requires Windows x64 and .NET SDK 10.0.400 (the published app bundles its runtime). From this folder:

```powershell
.\init.cmd                  # Restore, build, and run self-checks
.\petcompanion.cmd preview   # Open the interactive 3D viewer
.\petcompanion.cmd run       # Open the desktop companion
.\petcompanion.cmd test      # Self-checks, muted UI previews, audio playback checks
.\petcompanion.cmd publish   # Bundle the Windows runtime
.\petcompanion.cmd install   # Install published app and shortcuts
```

To update, quit the installed app, publish again, then run the install command. Settings are stored separately in `%LOCALAPPDATA%\Petcompanion\settings.json`. The app lives in `%LOCALAPPDATA%\Programs\Petcompanion`.

No network calls, credentials, telemetry, or paid services are used by the running app. The project is separate from PwdVault and does not read vaults.

## Inspect and export the 3D model

Double-click `Open-3D-Preview.cmd` to open an interactive model viewer. Drag to orbit, scroll to zoom, pause the live geometry, or export its current pose to GLB. The viewer saves its exports in `artifacts/dev/model-preview`.

The initial portable model is also in `src/Pets/Dragon/Exports/Dragon-static.glb`, with a preview and Khronos validator report (zero errors or warnings). This file contains colored geometry in one evaluated pose. It does not contain a skeleton or animation clips; the viewer's animation is implemented in C#. It is a basic stylized model, not a detailed Shenron asset. The file follows the [glTF 2.0 specification](https://registry.khronos.org/glTF/).

### Concept images

`src/Pets/Dragon/Assets/References/dragon.png` was generated with the built-in image-generation tool, using the user-supplied Shenron image as reference. Prompt: a transparent, full-body, compact coiled green Shenron with golden segmented belly, red eyes, brown antlers, long whiskers, small clawed arms, clean anime cel shading, commanding open-mouth expression, no scenery or text, and generous transparent margin for a desktop companion.

`src/Pets/Dragon/Assets/References/dragon-slither.png` was generated with the same built-in tool using the first image as reference. Prompt: eight different sequential frames, four columns by two rows, transparent cells, consistent Shenron character at the same scale, compact three-quarter view facing right, travelling body undulation, alternating reptilian leg paddles and tail sweep, no labels or grid lines. Both PNGs are reference/concept artwork only. The final app renders live 3D geometry and does not package these images.

Implementation references: [WPF windows](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/windows/) and [WPF multimedia](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/multimedia-overview).

## Repository and agent handoff

Everything needed to recreate this app lives in this repository. `init.cmd` is the first command after cloning; it requires the SDK in `global.json`. Run `petcompanion.cmd help` for the command list. Build/test logs and screenshots are under `artifacts/dev`; generated files are excluded from Git. The UI test briefly displays full-screen reminders and closes them automatically. Optional audio regeneration: `scripts/Make-Audio.ps1`.

- `AGENTS.md`: instructions for coding agents.
- `.agents/skills/recreate-petcompanion/SKILL.md`: reusable desktop-pet recreation skill.
- `docs/HANDOFF.md`: model/renderer map, visual goals, export limitations, and performance work.
- `scripts/Measure-Performance.ps1`: measure a running Petcompanion process's CPU and memory; see the handoff for methodology.
- `src/Pets/Dragon/Assets/` and `src/Pets/Dragon/Exports/`: required audio, visual references, and portable static model.

The next 3D agent can replace the prototype model and renderer. Detailed character fidelity and reduced resource consumption remain future work, with measurements required before claiming improvements.
## Custom reminders and presentation

Settings lets you add, edit, disable, or remove recurring custom messages (up to 240 characters), with intervals from 1 minute to 7 days. Save applies changes and restarts timers. Custom messages are displayed as text; recorded voices remain specific to water and movement.

Choose FullScreen or Popup for water and movement, and separately for each custom reminder. Popup is silent, appears in the lower corner of the pet monitor's working area, and does not activate or take keyboard focus. It supports Done, Skip, Snooze 5 min, and automatic dismissal. FullScreen retains the animated takeover. Quiet hours, pause, lock, and suspend apply to all automatic reminders. Reminders are spaced by at least two minutes after dismissal.

The app icon is an original ivory paw and copper reminder spark on a plum tile, shared by the executable, tray, and settings. Dragon, Orange cat, and Moon owl are selectable in this version. Future companions can implement the shared pet contracts described in docs/ARCHITECTURE.md; reminder records and scheduling contain no pet or renderer dependency.

## Source organization

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for folder ownership, the shared pet contract, and steps for adding a future pet.

Desktop pet size: use the 50-200% slider in Settings and Save. The preference persists across restarts and scales any pet through the shared host. Oversized pets are fitted to the monitor working area; full-screen reminders and compact popup sizes are independent.

Wander varies destinations across an outer band, with randomized inset and direction, rather than sticking to the border. It occasionally crosses the centre at higher speed (at most once per 90 seconds of active travel) and never targets a centre stop. Hover, drag, pause and reminders freeze route time. Follow and Stay retain their movement behavior. The dragon turns toward horizontal, vertical and diagonal travel. Large pets can still overlap central content; reduce size for more clearance.

Turning preserves the selected pet scale. The desktop host uses a square rotation area so vertical and diagonal flight do not trigger resizing. A real 3D turn may still change the projected silhouette; screen heading alone does not zoom the model.

The app palette uses warm ivory (#FAF4EB), plum (#382638), and copper (#9B472E), with blush surfaces. Shared colours and control styles live in src/UI/Ui.cs; editable logo artwork lives in Assets/Brand/logo.svg. The dragon retains its own character colours.

## Orange cat

Orange cat (2D) is the default cat: a rounded, vector-drawn tabby with mirrored left/right views, cream paws, pink ears, rosy cheeks, blinking eyes, a swishing tail and continuous walking/resting animations. Sleeping closes its eyes and adds floating z symbols; full-screen reminders show a raised paw and a heart. Orange cat (3D preview) preserves the previous model for comparison. Both share grounded movement, saved selection, size controls and gentle reminder sounds. Choose a version in Settings and Save. No image swapping or external assets are used.

## Interacting with the 2D cat

Click its body to pet it (closed eyes and floating hearts), or tap its nose for a boop. Clicking a sleeping cat wakes it. Drag starts only after moving the pointer beyond the normal drag threshold; double-click still opens Settings. Right-click the cat or tray icon for Pet the cat, Boop nose, Give a treat, Play with yarn, Take a nap, and Wake up. The same actions appear in Settings when the 2D cat is active. Treats show a food bowl and nibbling; yarn play adds a bouncing ball and pawing. Reactions pause ordinary wandering without changing your movement preference or reminder schedule. Nap lasts until an action wakes the cat; the other reactions end automatically. Dragging cancels a reaction. The 3D comparison cat retains its original behavior.

## Moon owl and the companion dashboard

Moon owl is an original animated vector companion with soft facial discs, amber eyes, blinking, head tilts, wing-assisted hops and feather details. Click to ruffle its feathers or boop its beak. Treats bring berries; Play brings a firefly; nap and wake actions work like the cat. It stays near the bottom rather than flying across your work, and its full-screen reminders use a soft hoot.

Settings now has an independent live preview, grouped cards, a fixed Save footer and an Up next reminder status. Pick a pet to preview it, then Save to switch the desktop companion. Changing the preview alone does not switch pets.

Use the **Focus** shortcut for 15-, 25-, or 45-minute sessions. The pet settles in a bottom corner while automatic reminders use silent corner popups. Your saved movement, audio and reminder style remain unchanged, and normal movement returns when focus ends. A silent completion nudge appears when the timer finishes. Focus pauses during app pause, Windows lock and suspend; it resumes only when those conditions clear. Quiet hours still suppress automatic reminders.

Enable **Let clicks pass through the pet** under Preferences to use windows behind the companion. Restore interaction from the tray menu or Settings. The preference is saved separately from focus; focus never changes it.

The existing pets also have refreshed details: softer curved tabby markings and ear reactions in 2D, a rounder 3D comparison cat, and richer dragon eyes, brows, whiskers, emerald/gold colouring and belly segments. No runtime performance or battery improvement is claimed. Existing dragon GLB exports remain earlier static prototype poses.

## App rename compatibility

The app is now Petcompanion. Build/run with `petcompanion.cmd` and `Petcompanion.csproj`; published files include `Petcompanion.exe`. The old `dragonbreak.cmd` forwards to the new launcher. Settings are saved under `%LOCALAPPDATA%\Petcompanion\settings.json`. If that file is absent or unreadable, the app reads the previous `%LOCALAPPDATA%\DragonBreak\settings.json` so your pets, reminders and audio choices remain available. Saving writes the new location. Old settings are not deleted. The single-instance lock remains compatible with older builds; quit the older app before opening Petcompanion. The GitHub repository name is unchanged.

## Sound

Settings has a **Test sound** button that applies the selected volume and plays the companion cue even during quiet hours. Mute and volume zero disable that button. Full-screen dragon reminders use roar/voice; cat and owl use original synthesized mew/hoot cues. Manual pet/boop/treat/play/wake interactions on the 2D cat and owl also play their cue, with a short cooldown. These are synthesized sounds, not real animal recordings; the on-screen purr remains text rather than a separate recording. Corner popups and automatic idle animations remain silent. Quiet hours stop automatic reminders, not manual sound tests. `petcompanion.cmd sound` runs a short 35%-volume owl test. `petcompanion.cmd test` verifies every shipped audio clip without audible playback. Regenerate the new cues offline with `scripts/Make-Pet-Audio.ps1`.

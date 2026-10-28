---
name: recreate-petcompanion
description: Recreate, run, package, or improve this repository's offline Windows dragon desktop pet, including articulated 3D motion, spoken break reminders, model preview/export, and measured resource use.
---

# Recreate Petcompanion

Use the repository containing this skill as the working root. Its `Petcompanion.csproj` is the app; no parent repository is required. Read `README.md` for commands and `docs/HANDOFF.md` before replacing the model/renderer or optimizing resource use.

## Recreate the desktop pet

1. On Windows x64, run `init.cmd`. It verifies the pinned .NET SDK, restores/builds, and runs self-checks. If the SDK is absent, report the required version from `global.json`; setup does not silently install machine-wide software.
2. Run `petcompanion.cmd preview` to inspect the live 3D geometry, or `petcompanion.cmd run` for the actual desktop companion. The pet/tray opens settings; preview windows are separate processes.
3. Run `petcompanion.cmd test`. UI checks briefly cover the current monitor with muted reminders. Preserve the user's own running instances and read all result logs.
4. For a requested installation, run `petcompanion.cmd publish`, then `petcompanion.cmd install` after the user has quit the installed pet. The installer creates per-user shortcuts and preserves the separate settings folder. Launch via the shortcut when a visible desktop pet is wanted.

## Preserve the intended experience

- Animate a real articulated model continuously, including body, tail, legs, and turns. The reference PNGs establish appearance, not the final animation mechanism.
- Keep curved edge-to-centre flight and optional pointer following usable. Avoid focus stealing during ordinary movement.
- Keep full-screen reminders dismissible through Esc/Skip, with Snooze, countdown, volume/mute and quiet hours. Keep speech and roar local; the supplied WAV files let recreation work without a voice-generation account.
- Describe static exports honestly. The shipped GLB has neither a rig nor animation clips. If producing an animated replacement, include editable sources, the rig and clips, then verify it in an actual viewer and integrate a compatible renderer.

## Improve rather than preserve the prototype

The next agent may replace the procedural mesh and renderer to achieve the user's desired appearance and efficiency. Use the file map and known limitations in `docs/HANDOFF.md` to choose the seam. Take before/after measurements with `scripts/Measure-Performance.ps1` for comparable scenarios; supplement with GPU/power/frame-time tools when making those claims. Report visual and resource tradeoffs explicitly.

Keep all source, required assets, deliberate exports, and recreation instructions in this repo. Leave build outputs, local profiles, and credentials ignored. Use the GitHub destination supplied by the user; preparing this skill does not authorize a different remote or an unsolicited push.

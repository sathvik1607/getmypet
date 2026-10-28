# Petcompanion

This folder is an independent Windows desktop-pet repository. Keep all build and asset paths relative to it; the parent checkout is unrelated.

- Start with `README.md` for commands. For 3D replacement, export, or performance work, read `docs/HANDOFF.md`.
- User intent: a recognizable Shenron-style dragon with continuous articulated motion, wandering through varied outer-screen positions with brief centre crossings, and offline spoken break reminders. The user rejected moving still images and swapping sprite frames as the final animation.
- Runtime code uses WPF/.NET; PNGs in `src/Pets/Dragon/Assets` are visual references. `src/Pets/Dragon/Exports/Dragon-static.glb` is a static pose with no rig or clips. Represent those limitations accurately.
- Keep reminder dismissal (Esc/Skip/Snooze), volume/mute, quiet hours, and session/power behavior working when changing the renderer. Test mode uses isolated in-memory settings and muted audio.
- Use `.agents/skills/recreate-petcompanion/SKILL.md` when recreating this pet, packaging it on another Windows machine, or replacing its model.
- `petcompanion.cmd test` runs the meaningful behavior checks. It displays temporary reminder windows. Close only test processes that the current task launches; the user may have another pet/viewer running.
- Profile before calling an optimization successful. Frame rate, triangle count, or file size alone do not establish CPU/GPU/battery improvement.
- Keep `bin`, `obj`, `artifacts`, local settings, and credentials out of commits. Commit source, required audio, visual references, and deliberate portable exports.

- Read `docs/ARCHITECTURE.md` before adding a pet. Keep pet-specific models, effects, audio and references within `src/Pets/<Pet>/`; shared UI depends on the pet interfaces.

- Orange cat lives in `src/Pets/Cat/`. Keep it grounded and upright; use `GroundMotion` and the pet activity contract instead of applying dragon flight orientation. Keep live pet switching, saved IDs and cat reminder and sound behavior covered by tests.

- Moon owl lives in `src/Pets/Owl/` and uses the existing grounded/activity/interaction interfaces. Focus sessions must never mutate saved movement/audio/reminder preferences. Keep click-through recoverable from the tray. Session-lock and power suspension are separate conditions; test their overlap.

# eXSert

_eXSert_ is a third-person character-action prototype set aboard a derelict research vessel. Master stance-based combos, aerial launchers, and precise guarding to carve through waves of autonomous security drones while the ship streams in around you.

---

## Installation & Launch
1. Download the latest build zip from the team share or itch.io page.
2. Extract the archive anywhere outside `Program Files` (write-access is required for saves).
3. Run `eXSert.exe`. The launcher boots directly into the main menu; all gameplay scenes stream additively via `SceneLoader`.

---

## Art & Asset Attribution

### Created In-House
- Player combatant mesh, rig, and the full animation set (combos, guard, air dash, plunge).
- Environment kits for the Elevator tutorial, Bridge connector, and Hangar arena, plus bespoke props and set dressing under `Assets/eXSert Assets/`.
- Enemy variants (Boxer, Crawler, Drone) including VFX hooks, UI, HUD, menus, and original SFX layers.
- All narrative UI art (objective widgets, dialogue cards, logbook entries) and gameplay shaders marked inside `Assets/eXSert Assets/Shaders`.

### External Content & Credits
- **Unity Technologies** – Starter Assets packs, Input System samples, Polybrush, ProBuilder, Cinemachine, Unity Toon Shader samples.
- **Adobe / Allegorithmic** – Adobe Substance 3D integration packages housed under `Assets/Adobe`.
- **Mirza Beig** – _Distortion Shockwaves VFX_ pack.
- **Hovl Studio** – _Magic Effects Pack_ (URP).
- **Vefects** – Zap / Trails / Easy Shockwaves URP packs (see `Assets/eXSert Assets/VFX&Shader/[Imported VFX]/`).
- **Starter third-party audio stingers** bundled with the above packs (see individual license files within each folder).

All third-party folders keep their original license/readme files; please review them before redistributing the project.

---

## Major Mechanics Snapshot
- **Stance Switching** – Tap `LB/L1` (controller) or `Q` (keyboard) to swap between single-target and AOE move lists. Stances drive combo trees, hitbox sizes, and animation layers.
- **Combo System** – Light strings chain into heavy finishers. ScriptableObject `PlayerAttack` assets define per-attack hitboxes, VFX anchors, target caps, and SFX.
- **Aerial Combat & Plunge** – Launchers send enemies airborne; follow up with air lights/heavies or trigger the plunge (heavy aerial) to slam into clustered crowds.
- **Guard & Parry Focus** – Holding Guard locks movement speed, pivots the camera, and opens guard-specific dash cancels plus a special counterattack window.
- **Additive Scene Streaming** – `SceneLoader` keeps the elevator hub resident while additively loading bridge/hangar chunks based on triggers.
- **Checkpoint / Persistence** – `CheckpointSystem` writes scene + spawn IDs to save slots; reloading spawns you through `PlayerSpawner` at the last triggered checkpoint.

---

## Controls

| Action | Keyboard & Mouse | Xbox / PlayStation |
| --- | --- | --- |
| Move / Look | `WASD` / Mouse | Left Stick / Right Stick |
| Light Attack | `LMB` / `X` key | `X` / `Square` |
| Heavy Attack | `RMB` / `Y` key | `Y` / `Triangle` |
| Jump / Double Jump | Space | `A` / `Cross` |
| Dash / Air Dash | Left Shift | `B` / `Circle` |
| Guard (Hold) | Right Mouse / `Mouse4` (bindable) | `RB` / `R1` |
| Change Stance | `Q` or `LB` (if using pad prompts) | `LB` / `L1` |
| Lock-On Toggle | `Middle Mouse` | `RS Click` |
| Pause / Menu | `Esc` | `Start / Options` |

Rebinding is done through the Input System settings panel (Pause → Settings → Controls) and writes to `InputSystem.inputsettings.asset`.

---

## How to Beat the Current Build

1. **Elevator Tutorial (FP_Elevator)**
   - Follow the on-screen objectives. Three enemy waves spawn sequentially: light-attack tutorial, heavy-attack tutorial, then stance-change tutorial.
   - Use the objective notices to learn each mechanic; the elevator doors open once Wave 3 dies. Step into the lit pad to trigger the additive load for Bridge.

2. **Bridge Connector (FP_Bridge)**
   - Upon loading, you face a short traversal space with mixed Boxer/Crawler patrols. Use guard-walk (hold Guard + move) to inch forward while blocking turret fire.
   - Activate the bridge console (prompt appears when close). This spawns the Hangar chunk additively and opens the blast doors.

3. **Hangar Arena (FP_Hangar)**
   - Clear the initial ring of drones to unlock the center console.
   - Interact with the console to trigger the multi-wave gauntlet (AOE stance excels here). Guard dashes give i-frames—use them to slip past laser barrages.
   - The encounter ends after the fifth wave (two Boxers + drone swarm). A checkpoint trigger behind the hangar doors fires automatically; proceed to the exit waypoint to finish the build.

4. **Fail States**
   - If you die, select “Restart from Checkpoint” to reload the active scene/spawn point via `SceneLoader`. The tutorial resets to Wave 1; the hangar resumes at the latest checkpoint.

Tips: Light attacks can be animation-cancelled into dash/guard, aerial heavies have built-in launch, and plunge slams now cap at five targets to keep performance stable.

---

## Debug Tools & Cheats
- **God Mode Toggle** – In the Unity Editor select the player, enable `PlayerHealthBarManager → Invulnerable`. Persisted through play mode but not exposed in builds.
- **Context Menus** – `PlayerHealthBarManager` offers “Debug/Apply Damage” and “Debug/Kill Player” context buttons; boss brain/health scripts expose similar test hooks.
- **Checkpoint Reset** – `CheckpointSystem` inspector has buttons for clearing saves/testing load flows; `SceneLoader`’s debug logging can be enabled per component.
- **Temp VFX Switcher** – `TempAoEVfxSwitcher` lets you toggle rig-mounted VFX for AoE/plunge as well as double jump/air dash when testing presentation changes.

These debugging aides are editor-only and should be disabled or hidden before shipping.

---

## Known Issues (Dec 2025)
- **Animation Edge Cases** – Rapidly spamming dash/guard can desync locomotion layers before they resync; cancel out by releasing guard for ~0.5 s.
- **Controller Reconnect** – Unplugging a gamepad while paused sometimes leaves the Input System in keyboard mode until you reopen the pause menu.
- **Scene Streaming Pops** – If you sprint through a transition trigger twice in the same frame, an additive scene can unload before the next one finishes streaming.
- **Missing VFX Anchors** – Attacks without a configured `PlayerVfxAnchorRegistry` entry fall back to the hitbox origin (looks like ground sparks). Configure anchors on the player rig to fix.
- **AI Awareness** – Swapped-in enemies occasionally spawn with zero velocity on sloped navmesh tiles; they unstick after taking damage but remain stationary otherwise.

Please document any additional issues in the project tracker so QA can repro them.

---

## AI Disclosure

Portions of the combat scripts, additive loading utilities, VFX toggles, and this README were authored with help from **GitHub Copilot (GPT-5.1-Codex Preview)**. The tool was used to scaffold C# methods, refactor attack/VFX systems, and draft documentation, with every change reviewed and adjusted by the development team.

---

## Build Info
- Engine: Unity 6000.2.6f2 (2025.2 LTS equivalent)
- Platform: Windows 10/11 (DX11 URP)
- Current Milestone: Vertical Slice (Elevator → Hangar)
- Last Content Update: December 2025

Enjoy the slice, and reach out on Teams if you need anything else!


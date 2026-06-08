# Outshipped

Outshipped is a Unity courier-combat prototype about driving a weaponized delivery car through a stylized arena, collecting packages, delivering them to depots, and surviving hostile spider enemies long enough to build a high score.

![Outshipped intro art](Assets/Textures/intro.png)

## Highlights

- Arcade vehicle handling built with a custom raycast-wheel suspension system instead of Unity `WheelCollider`s.
- Mouse-aimed turret combat with hitscan tracers, muzzle flashes, impact effects, ammo, reloads, and weapon-specific audio.
- Courier loadouts that change the run before it starts: Workhorse, Cruiser, Freighter, and Monarch each select different turret stats, magazine sizes, passives, ranges, fire rates, and tracer colors.
- Delivery loop with base pickup, depot drop-off, package visuals, delivery scoring, and passive effects like healing or duplicated deliveries.
- Pickup-driven active abilities including Amped damage, Overdrive speed, EMP, Slow field, and armor stacks.
- Runtime HUD built with UI Toolkit for health, armor, ammo, packages delivered, courier/passive display, ability state, popups, and game-over transition.
- ML-Agents spider enemy experiment with configurable joints, curriculum training, chase-target observations, gait bias, self-righting, and training metrics.

## Overview

Outshipped mixes three prototype loops into one playable Unity project:

1. Drive a cartoony physics vehicle around the map.
2. Shoot spiders with a roof-mounted turret while managing magazine and reload timing.
3. Pick up cargo at the base, deliver it to depots, and use pickups to stay alive longer.

The project is made by Ryan Xu, Leo Xia, Alvin Zhang, Dorian Choy, and William Zhang as a learning prototype, so the code favors readable gameplay systems and inspector-tunable values over production-level architecture. Most of the interesting implementation lives in `Assets/Scripts`, `Assets/Scripts/Car`, and the UI Toolkit folders under `Assets/UI`.

## Gameplay

Start from the pre-game flow, choose a courier, then enter the `InGame` scene. Your selected courier determines the weapon and passive ability used for the run:

| Courier | Passive | Turret | Notes |
| --- | --- | --- | --- |
| Workhorse | Heal on Delivery | Automatic | Balanced magazine, rate of fire, and range. |
| Cruiser | Lifesteal on Kill | Buckshot | Shorter range shotgun-style turret with multiple pellets. |
| Freighter | Delivery Duplication | Piercing | Slow, high-damage shots and a chance to count extra deliveries. |
| Monarch | XP Multiplier | Rapid-Fire | Large magazine, very fast fire rate, and score multiplier. |

During the run, the scoring system combines packages delivered, enemies destroyed, active abilities used, and time survived. Game over calculates the final score and passes it to the post-game UI.

## Controls

| Action | Input |
| --- | --- |
| Drive | Keyboard movement input through Unity Input System |
| Aim turret | Mouse position |
| Fire turret | Left mouse button |
| Reload | `R` |
| Use stored ability | `E` |

## Installation

Use Unity `6000.4.4f1` or newer in the Unity 6 line.

```bash
git clone <repo-url>
cd Outshipped
```

Open the folder in Unity Hub, let Unity restore packages from `Packages/manifest.json`, then open `Assets/Scenes/PreGame.unity` or `Assets/Scenes/InGame.unity`.

Important package dependencies include:

- Universal Render Pipeline `17.4.0`
- Input System `1.19.0`
- Cinemachine `3.1.6`
- ML-Agents `4.0.3`
- Visual Effect Graph and Shader Graph `17.4.0`
- UI Toolkit / UI Elements

## Project Structure

```text
Assets/
  Scenes/                  Main prototype scenes, including PreGame, InGame, PostGame, and AI tests
  Scripts/                 Gameplay managers, pickups, scoring, delivery, and spider AI
  Scripts/Car/             Vehicle physics, player input, turret, projectiles, VFX, and spider health
  UI/                      UI Toolkit HUD, title, menu, selection, scoring, manual, and settings screens
  Prefabs/                 Player, turret, spiders, cargo, pickups, and VFX prefabs
  ML-Agents/               Training timer output and AI experiment artifacts
  Models/, Materials/      Stylized map, prop, foliage, and surface assets
```

## Technical Notes

The car uses `CarController` and `WheelPhysics` together. `CarController` calculates wheel layout, Ackermann steering, throttle smoothing, speed limiting, anti-roll forces, downforce, yaw damping, and collision recovery. `WheelPhysics` handles raycast suspension, compression, contact normals, lateral grip, longitudinal drive/brake forces, curb recovery, and wheel mesh animation.

The turret is handled by `TurretController`. It uses the Unity Input System for cursor aiming, Cinemachine for aim-follow camera feel, clamped yaw/pitch pivots, hitscan raycasts, tracer visuals, impact particles, muzzle flashes, and buckshot pellet spread.

The run state is coordinated by `gameManager`. It initializes courier loadout data from `GameSelection`, updates HUD modules, manages ammo/reloads, stores and activates pickups, applies health and armor, handles ability vignettes/FOV changes, and triggers final scoring.

The spider experiment lives in `SpiderAgent`. It uses ML-Agents observations/actions with configurable joints, runtime grip materials, curriculum settings, target prediction, target-facing rewards, progress rewards, self-righting helpers, and debug statistics.

## Development

For a quick script compile check outside the Unity editor, run:

```bash
dotnet build Assembly-CSharp.csproj --no-restore
```

For gameplay testing, prefer the Unity editor because the prototype depends heavily on scene wiring, serialized inspector values, UI Toolkit documents, prefabs, layers, and package-restored Unity assets.

## Status

Outshipped is an active student prototype. Expect rough edges, inspector-driven tuning, experimental scenes, and generated Unity artifacts. The current strongest systems are the vehicle/turret feel, loadout selection, delivery scoring loop, pickup abilities, HUD feedback, and spider AI training exploration.

## Contributing

This project is mainly a learning workspace. Useful contributions or review notes should focus on:

- Keeping vehicle changes compatible with the custom raycast-wheel setup.
- Preserving the cartoony handling feel while simplifying tuning where possible.
- Improving delivery/combat clarity in the current loop.
- Making scene wiring, prefabs, layers, and serialized references easier to understand.
- Adding small, testable gameplay features before large rewrites.

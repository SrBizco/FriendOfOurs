# Friend Of Ours

**Friend Of Ours** is a top-down open-city action prototype developed in Unity. Explore the city on foot, possess street props, drive vehicles, fight NPCs, evade police, and enter the gun store through an additive scene transition.

## Play the Game

- [itch.io page](https://maxigianetti.itch.io/friendofours)
- [Game Design Document](https://docs.google.com/document/d/1pd9wQErYp-VNxJZoZkWbkmSqSplFFsSaYEe3fCuQLPM/edit?tab=t.0#heading=h.988uv5he6yq2)

## How to Play

| Input | Action |
| --- | --- |
| `WASD` / Arrow Keys | Move the character or drive a vehicle |
| `Left Shift` | Toggle walk/run |
| `Space` | Jump / handbrake while driving |
| Left Mouse Button | Punch |
| `E` | Enter or leave a vehicle; interact with doors |
| `F` | Possess a nearby prop |
| `G` | Return from prop possession |
| `Escape` | Open or close the pause menu |
| Right Mouse Button | Rotate the camera while on foot |

## Gameplay Features

- Third-person/top-down movement driven by a finite state machine.
- Configurable consecutive jumps and slope limits.
- Surface-dependent footsteps.
- Prop possession system.
- Driveable civilian vehicles and NPC traffic.
- Pedestrians that wander, flee, or fight back.
- Health, death, respawn, and money rewards for defeated NPCs.
- Wanted system: defeating NPCs raises the wanted level and spawns police officers that pursue the player.
- Additive Gun Store interior with asynchronous loading and a fake loading bar.
- Main menu, settings, credits, pause menu, HUD, and shared audio settings.
- Spatial vehicle audio, footsteps, combat audio, and menu music.

## Project Information

- **Engine:** Unity 2023.2.20f1
- **Genre:** Top-down open-city action prototype
- **Platform:** Windows
- **Developer:** Maxi Gianetti

## Credits

- Developed by Maximo Gianetti.
- UI created with **Modern UI Pack** by Michsky.
- City environment assets include **PolygonCity** and **TurnTheGameOn / Racing City** packages.
- Additional animations, models, sounds, and third-party assets remain the property of their respective authors and are used under their corresponding licenses.


For the full design, development goals, and weekly sprint planning, see the Game Design Document https://docs.google.com/document/d/1pd9wQErYp-VNxJZoZkWbkmSqSplFFsSaYEe3fCuQLPM/edit?tab=t.0#heading=h.988uv5he6yq2

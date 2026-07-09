# Plant Grass

Repaint grass onto terrain that's turned to bare dirt/mud from trampling — press a key, get your grass back.

## Requirements

- [BepInEx (IL2CPP)](https://www.nexusmods.com/aska/mods/66) must be installed first.

## Installation

Extract the archive into your Aska game folder (same folder as `Aska.exe`), so the DLL ends up at:
`BepInEx\plugins\AskaPlantGrass\AskaPlantGrass.dll`

## Usage

Stand near a dirt/mud patch and press `]` (configurable). Grass is restored in a radius around you.

## Configuration

After first launch, edit `BepInEx\config\smithio.aska.plantgrass.cfg`:

| Setting | Default | Description |
|---|---|---|
| `Enabled` | `true` | Master on/off switch |
| `Key` | `RightBracket` | Key that paints grass at your feet |

The paint radius (1m) is fixed and not configurable.

## Known issues

- **Painted grass does not currently persist across a game restart** — it reverts to dirt after closing and relaunching the game. Being investigated.

## Multiplayer

Built using the same local-authority pattern as other Aska terrain mods, which should replicate the change to other connected players automatically. This has only been confirmed in singleplayer so far — treat multiplayer as untested until reported otherwise.

## Source

https://github.com/msmithi33/aska-plant-grass

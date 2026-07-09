# Aska Plant Grass

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for [Aska](https://store.steampowered.com/app/2823370/ASKA/) that lets you repaint grass back onto terrain that's turned to bare dirt/mud from trampling.

## Usage

Press `]` (configurable) near a dirt patch to restore grass in a radius around you.

Roads, paths, and bedrock are left untouched, and the tool won't paint at all if a building is within range — only trampled dirt gets grassed over.

## Install

1. Install BepInEx 6 (IL2CPP) into your Aska game folder.
2. Drop `AskaPlantGrass.dll` into `BepInEx\plugins\AskaPlantGrass\`.
3. Launch the game once to generate `BepInEx\config\smithio.aska.plantgrass.cfg`.

## Config

| Setting | Default | Description |
|---|---|---|
| `Enabled` | `true` | Master on/off switch |
| `Key` | `RightBracket` | Key that paints grass at your feet |

The paint radius (1m) is fixed, not configurable — see [DEVELOPMENT.md](DEVELOPMENT.md) for why.

## Multiplayer

The tool only ever runs against the local player's own character (gated on `IsPlayer()` and `GetLocalAuthorityMask() == 1`), the same pattern used by other Aska mods with similar terrain tools. This *should* replicate to other connected players via the game's own Photon Fusion networking, but has only been verified in singleplayer so far — multiplayer behavior is currently untested.

## Development

See [DEVELOPMENT.md](DEVELOPMENT.md) for environment setup, build/deploy steps, and the reasoning behind the trickier bits of the code.

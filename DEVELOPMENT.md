# Development Notes

Context for anyone (including future-me) picking this project back up.

## Environment

- Game: Aska (Sand Sailor Studio), Unity **IL2CPP**, install path referenced via the `AskaInteropDir` MSBuild property in the `.csproj` — change that one property if the game lives somewhere else.
- Modding stack: **BepInEx 6 (IL2CPP, Bleeding Edge)**. Must already be installed in the game folder (generates `BepInEx\interop\*.dll` stub assemblies that this project references at `Private=False` — they're compile-time stand-ins for the real game types, not shipped code).
- Networking: **Photon Fusion**. Grass rendering sits on Vegetation Studio Pro, driven by the game's own `TerraformingMap`/`HeightmapTool` system.
- Toolchain: .NET SDK targeting `net6.0`, any editor (VS Code + C# extension is enough, no full Visual Studio needed).

## Build & deploy loop

```
dotnet build -c Release
```

Then copy `bin\Release\net6.0\AskaPlantGrass.dll` into `<game>\BepInEx\plugins\AskaPlantGrass\`.

**The DLL is locked while Aska is running** — close the game before overwriting it, or the copy fails with "Device or resource busy" / access denied.

To check the plugin actually loaded and ran without exceptions, tail `<game>\BepInEx\LogOutput.log` and look for lines under the `Aska Plant Grass` log category.

## Why the code looks like this

- **`Character` lives in the `SSSGame` namespace** in `Assembly-CSharp.dll` — not discoverable from the type name alone, found by reading a working reference mod's `using` statements.
- **`Fusion.Runtime.dll` must be referenced even though no code names a Fusion type** — `Character` inherits Fusion's `NetworkBehaviour`, which is where `GetLocalAuthorityMask()` is actually defined. Drop the reference and the build fails with `CS0012`.
- **`ClassInjector.RegisterTypeInIl2Cpp<T>()`** is only needed for *new* MonoBehaviour-derived types we author (`PlantGrassTool`). Pre-existing game types like `HeightmapTool` don't need it — IL2CPP already has metadata for them.
- **Gating on `IsPlayer() && GetLocalAuthorityMask() == 1`** in `Character.Spawned` is how the tool ends up attached only to the local player's own character, not NPCs or (in multiplayer) other players' characters. Confirmed empirically in-game: in a singleplayer session, `GetLocalAuthorityMask()` reads `1` for every character (villagers included) since the local session has authority over everything — `IsPlayer()` is what actually distinguishes "me" from NPCs there. The real multiplayer discriminating power of the mask is unverified (see Multiplayer below).
- **Radius is fixed at 1m (`PlantGrassTool.RadiusMeters`), not configurable.** Originally made configurable, then reverted after failing to get it working - see the "Radius/Size configurability" section below for the full story and why it's not worth chasing further without native decompilation tools.
- Avoided `Transform.CreateChild(...)` (used by the reference mod) in favor of plain `new GameObject` + `SetParent` — that extension method lives in `SandSailorStudio.dll`, a reference this project deliberately doesn't pull in to keep the dependency list minimal.
- **Paint is restricted to cells currently typed `DIRT`** (`HeightmapTool.requiresTerrainType`/`requiredTerrainType`), not a blanket paint over the whole radius. `TerraformingMap.TerrainType` is `{ NATURAL = 0, DIRT = 1, BEDROCK = 2, PATH = 3, ROAD = 19 }` — restricting to `DIRT` means roads/paths/bedrock/already-natural ground are never touched, as a side effect of only ever doing the thing the mod is actually for.
- **Buildings are not excluded by terrain type** — the ground under a placed structure is still typed `DIRT`/`NATURAL` underneath it, so the type restriction above doesn't stop grass from being painted under a building. Fixed separately: `Physics.OverlapSphere(position, radius)` and skip the whole paint if any overlapping collider has `SSSGame.Structure` (a `NetworkComponent`) in its parent chain. Needed adding the `UnityEngine.PhysicsModule` interop reference.
- **The overlap check must filter to `Structure.c_FootprintTag`, not just "any collider under a Structure".** First version matched any collider under a `Structure` and produced false positives ~8.5m from a campfire — `Structure` instances can carry other, much larger colliders (interaction range, heat/warmth radius) that aren't their physical footprint. Diagnostic logging (structure name via `GetName()`, the collider's own name/position, distance) is what surfaced this — the log named the actual offending collider, which turned out not to be tagged as the footprint. Filtering with `overlap.CompareTag(Structure.c_FootprintTag)` before the `Structure` check fixed it; confirmed in-game the false positive is gone and real building footprints (named `Footprint` in the log) still correctly block.
- **How the enum and field semantics were actually confirmed**, since the interop DLL only exposes IL2CPP call-trampoline signatures (no method bodies — those run in native `GameAssembly.dll`, which ILSpy can't decompile): installed `ilspycmd` (`dotnet tool install -g ilspycmd --version 8.2.0.7535` — the latest version failed to install with a "DotnetToolSettings.xml was not found" error on this machine) and ran it directly against `Assembly-CSharp.dll` in the game's `BepInEx\interop` folder, e.g. `ilspycmd -t "SSSGame.TerraformingMap" ...\Assembly-CSharp.dll`. This gives real field/enum declarations (like the `TerrainType` values above), but only trampoline stubs for method bodies — so field *names* and *types* are ground truth, but exactly how a native method uses them (e.g. whether `requiresTerrainType` gates per grid-cell or just once for the whole call) is inferred from naming and confirmed empirically in-game, not read from source.

## Radius/Size configurability (tried and reverted)

Attempted to make the paint radius configurable and never got it working, despite three different approaches, each seemingly reasonable from the decompiled declarations:

1. Set `HeightmapTool.radius` directly from a config value. No visible effect on painted area size.
2. Discovered `HeightmapTool` also has `SetSize(TerraformingToolSize)` (`TerraformingToolSize` is `{ SMALL, MEDIUM, MEDIUM_SOFT, EXTRA_LARGE }`), backed by a `Config`/`_activeConfig`/data-buffer system (`HeightmapTool.Config` has `size`, `brush`, `data`, `CreateData(...)`). Hypothesis: `SetSize()` builds the actual data buffer the paint operation iterates over, so `radius` alone doing nothing made sense if the buffer size never changed. Switched the config to drive `SetSize()` instead. Still no visible effect, confirmed in-game with `Size = EXTRA_LARGE`.
3. Tried both together (`SetSize()` *and* `radius` set from a size-to-meters mapping). Still no visible effect.

At that point a confound surfaced: testing had been happening repeatedly in the same small spot across many earlier rounds of testing (Structure exclusion, footprint-tag fix, etc.), which could have masked a real change if that patch was already fully converted to `NATURAL`. That turned out not to be the actual explanation either — **the real cause is that painted grass isn't persisted between game launches** (see below), so every test was unknowingly starting from a fresh, unmodified DIRT patch each time, and "no visible difference" was real, not a testing artifact.

Given three failed hypotheses and no way to decompile the native method bodies that actually determine paint extent (would need Il2CppDumper + a disassembler against `GameAssembly.dll`, well beyond what's reasonable for this feature), radius/size configurability was dropped. The paint radius is fixed at `PlantGrassTool.RadiusMeters = 1f`, matching the reference mod's own hardcoded value, which is the one value empirically confirmed to work.

## Known issue: painted grass doesn't persist across game restarts

Discovered while debugging the radius issue above: grass restored by this mod reverts to `DIRT` after closing and relaunching the game (confirmed by the user testing the same spot repeatedly across sessions and always finding it back to bare dirt). This means `HeightmapTool.Run()`/`PaintHere()` mutate the in-memory `TerraformingMap` but that change isn't being written into the save data the way the game's own terrain edits are. Not yet investigated further - the likely next step is figuring out what part of Aska's save/serialization path terrain-type changes are supposed to go through (possibly something the game's own terraforming tools trigger via a save/dirty-flag call that this mod isn't calling), but that needs its own investigation pass.

## Reference implementation

[`radekkpl/askaplus.bepinex.mod`](https://github.com/radekkpl/askaplus.bepinex.mod) ships a near-identical "paint grass on keypress" feature and was used as a working reference for the game API surface (`HeightmapTool`, `TerraformingMap.TerrainType`, `Character.Spawned`). This project is not a fork or dependency of it — just inspired by it, trimmed to only what this feature needs.

## Multiplayer status

Built to the same local-authority-gated pattern as the reference mod, which should replicate the terrain change to other connected players via Fusion without any manual networking code — but this is **unverified**. The reference mod's own README says it's only tested singleplayer, and there's no confirmed report (from that project's issue tracker or elsewhere) of this specific feature working multiplayer. Only singleplayer has been directly tested here. If you get a chance to test with a second client or dedicated server, that's the next real gap to close.

## Verification checklist (last run manually, in-game)

- [x] Plugin loads (`LogOutput.log` shows load line, no errors)
- [x] Harmony patch fires once per `Character.Spawned`, correctly distinguishes the local player from NPCs
- [x] Pressing the configured key near dirt/mud visibly restores grass in singleplayer
- [x] Config file generates with `Enabled`/`Key` in one `[PlantGrass]` section
- [x] Roads/paths are left untouched when painting dirt right next to them
- [x] Painting near a building is skipped entirely, no grass appears under/through it
- [ ] Multiplayer replication (needs a second tester)
- [ ] Grass persists across a game restart (currently known broken, see above)

## Publishing

Packaging assets for Thunderstore (`thunderstore/manifest.json`, `thunderstore/icon.png`) and Nexus Mods (`nexusmods/page-description.md`) are checked in. Built zips (`*.zip`) and the copied DLL/README used only for packaging are gitignored — regenerate them from `bin/Release` before a release rather than trusting stale copies.

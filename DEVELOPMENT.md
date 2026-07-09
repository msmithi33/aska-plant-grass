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

Then copy `bin\Release\net6.0\AskaGrassRestore.dll` into `<game>\BepInEx\plugins\AskaGrassRestore\`.

**The DLL is locked while Aska is running** — close the game before overwriting it, or the copy fails with "Device or resource busy" / access denied.

To check the plugin actually loaded and ran without exceptions, tail `<game>\BepInEx\LogOutput.log` and look for lines under the `Aska Grass Restore` log category.

## Why the code looks like this

- **`Character` lives in the `SSSGame` namespace** in `Assembly-CSharp.dll` — not discoverable from the type name alone, found by reading a working reference mod's `using` statements.
- **`Fusion.Runtime.dll` must be referenced even though no code names a Fusion type** — `Character` inherits Fusion's `NetworkBehaviour`, which is where `GetLocalAuthorityMask()` is actually defined. Drop the reference and the build fails with `CS0012`.
- **`ClassInjector.RegisterTypeInIl2Cpp<T>()`** is only needed for *new* MonoBehaviour-derived types we author (`GrassRestoreTool`). Pre-existing game types like `HeightmapTool` don't need it — IL2CPP already has metadata for them.
- **Gating on `IsPlayer() && GetLocalAuthorityMask() == 1`** in `Character.Spawned` is how the tool ends up attached only to the local player's own character, not NPCs or (in multiplayer) other players' characters. Confirmed empirically in-game: in a singleplayer session, `GetLocalAuthorityMask()` reads `1` for every character (villagers included) since the local session has authority over everything — `IsPlayer()` is what actually distinguishes "me" from NPCs there. The real multiplayer discriminating power of the mask is unverified (see Multiplayer below).
- **Radius is configurable**, unlike the reference mod which hardcodes it — a deliberate one-line addition since it was low-risk and useful.
- Avoided `Transform.CreateChild(...)` (used by the reference mod) in favor of plain `new GameObject` + `SetParent` — that extension method lives in `SandSailorStudio.dll`, a reference this project deliberately doesn't pull in to keep the dependency list minimal.
- **Paint is restricted to cells currently typed `DIRT`** (`HeightmapTool.requiresTerrainType`/`requiredTerrainType`), not a blanket paint over the whole radius. `TerraformingMap.TerrainType` is `{ NATURAL = 0, DIRT = 1, BEDROCK = 2, PATH = 3, ROAD = 19 }` — restricting to `DIRT` means roads/paths/bedrock/already-natural ground are never touched, as a side effect of only ever doing the thing the mod is actually for.
- **Buildings are not excluded by terrain type** — the ground under a placed structure is still typed `DIRT`/`NATURAL` underneath it, so the type restriction above doesn't stop grass from being painted under a building. Fixed separately: `Physics.OverlapSphere(position, radius)` and skip the whole paint if any overlapping collider has `SSSGame.Structure` (a `NetworkComponent`) in its parent chain. Needed adding the `UnityEngine.PhysicsModule` interop reference.
- **The overlap check must filter to `Structure.c_FootprintTag`, not just "any collider under a Structure".** First version matched any collider under a `Structure` and produced false positives ~8.5m from a campfire — `Structure` instances can carry other, much larger colliders (interaction range, heat/warmth radius) that aren't their physical footprint. Diagnostic logging (structure name via `GetName()`, the collider's own name/position, distance) is what surfaced this — the log named the actual offending collider, which turned out not to be tagged as the footprint. Filtering with `overlap.CompareTag(Structure.c_FootprintTag)` before the `Structure` check fixed it; confirmed in-game the false positive is gone and real building footprints (named `Footprint` in the log) still correctly block.
- **How the enum and field semantics were actually confirmed**, since the interop DLL only exposes IL2CPP call-trampoline signatures (no method bodies — those run in native `GameAssembly.dll`, which ILSpy can't decompile): installed `ilspycmd` (`dotnet tool install -g ilspycmd --version 8.2.0.7535` — the latest version failed to install with a "DotnetToolSettings.xml was not found" error on this machine) and ran it directly against `Assembly-CSharp.dll` in the game's `BepInEx\interop` folder, e.g. `ilspycmd -t "SSSGame.TerraformingMap" ...\Assembly-CSharp.dll`. This gives real field/enum declarations (like the `TerrainType` values above), but only trampoline stubs for method bodies — so field *names* and *types* are ground truth, but exactly how a native method uses them (e.g. whether `requiresTerrainType` gates per grid-cell or just once for the whole call) is inferred from naming and confirmed empirically in-game, not read from source.

## Reference implementation

[`radekkpl/askaplus.bepinex.mod`](https://github.com/radekkpl/askaplus.bepinex.mod) ships a near-identical "paint grass on keypress" feature and was used as a working reference for the game API surface (`HeightmapTool`, `TerraformingMap.TerrainType`, `Character.Spawned`). This project is not a fork or dependency of it — just inspired by it, trimmed to only what this feature needs.

## Multiplayer status

Built to the same local-authority-gated pattern as the reference mod, which should replicate the terrain change to other connected players via Fusion without any manual networking code — but this is **unverified**. The reference mod's own README says it's only tested singleplayer, and there's no confirmed report (from that project's issue tracker or elsewhere) of this specific feature working multiplayer. Only singleplayer has been directly tested here. If you get a chance to test with a second client or dedicated server, that's the next real gap to close.

## Verification checklist (last run manually, in-game)

- [x] Plugin loads (`LogOutput.log` shows load line, no errors)
- [x] Harmony patch fires once per `Character.Spawned`, correctly distinguishes the local player from NPCs
- [x] Pressing the configured key near dirt/mud visibly restores grass in singleplayer
- [x] Config file generates with `Enabled`/`Key`/`Radius` in one `[GrassRestore]` section
- [x] Roads/paths are left untouched when painting dirt right next to them
- [x] Painting near a building is skipped entirely, no grass appears under/through it
- [ ] Multiplayer replication (needs a second tester)

## Publishing

Packaging assets for Thunderstore (`thunderstore/manifest.json`, `thunderstore/icon.png`) and Nexus Mods (`nexusmods/page-description.md`) are checked in. Built zips (`*.zip`) and the copied DLL/README used only for packaging are gitignored — regenerate them from `bin/Release` before a release rather than trusting stale copies.

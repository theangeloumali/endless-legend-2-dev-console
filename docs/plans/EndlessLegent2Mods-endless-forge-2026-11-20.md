# Endless Forge (GeloDGreat Dev Console 2.0) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: use superpowers:executing-plans (Route: inline, explained below) to implement this plan task by task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship console v2.0, "Endless Forge". It exposes the 61 editor commands the console doesn't use yet, adds a command search, an edit log with undo, and recipes (saved edit sequences that replay on a new game), and gets a public release.

**Architecture:**

- **Pure logic moves to a new library.** Recipes, hex math, the undo table, search ranking and the edit log go in a new `Forge.Core` library with no game references, so it can be unit-tested.
- **Game-facing code stays in `plugin/DevConsole`.** It follows the existing pattern: `Cheats/*.cs` posts orders, `Ui/Tab*.cs` draws IMGUI.
- **Every order passes through one choke point.** All orders go through `Orders.Post`, which becomes where the edit log and the recipe recorder hook in.

**Tech stack:**

- C# `net472`, BepInEx 5.4.23.5, HarmonyX, Unity IMGUI
- The game's public `EditorOrder*` DTOs (`Amplitude.Mercury.Interop`)
- Newtonsoft.Json, which ships with the game, for recipe files
- xUnit for `Forge.Core`
- Build with `dotnet.exe` (Windows SDK) through `tools/build_plugin.py`

**Spec:** this document. The design and decisions are recorded under "Design" below. Supporting material:

- mock-up `docs/mockups/endless-forge-v2-mockup.html`
- `ZKidz/EndlessLegend2/claudedocs/research/unused-editor-order-fields-1.0.239.txt` (fields of every unused command)
- `ZKidz/EndlessLegend2/claudedocs/EndlessLegend2-top10-mod-proposals-2026-11-20.md` (proposal #1)

## Design

**Intent.** Your GeloDGreat Dev Console already covers economy, heroes, unit spawning and basic terraforming. Amplitude shipped 102 editor commands in the retail build, and the console uses 50 of them. Forge turns the console into a full single-player world editor and sandbox for public release (Nexus and GitHub). The reference point is ES2's Sandbox Mod, with 77,732 subscribers.

**Decided with Angelo (2026-11-20):**

| Question     | Answer                                                                                                                                                                                 |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Audience     | Public sandbox release: every command gets clear UI, safe defaults and readable errors                                                                                                 |
| Packaging    | Console v2.0: same DLL (`DevConsole.dll`), same Insert window, same config GUID `angelo.el2.devconsole`. The header reads `GELODGREAT DEV CONSOLE 2.0 · ENDLESS FORGE`                 |
| Verification | **Prototype session.** Tests, code review, Codex, /simplify and the E2E video are deferred to the settle phase (Phase 8) before merge. The build and the in-game check run every phase |
| Mock-up      | Approved before code: `docs/mockups/endless-forge-v2-mockup.html`                                                                                                                      |

**Approaches considered:**

1. **Console v2.0, same DLL. CHOSEN.** One plugin to keep in step with patches, existing users upgrade in place, and it reuses `Orders`, `Sim`, `Catalog`, `Widgets` and `Theme` as they are.
2. **A separate Forge plugin with a shared core.** Cleaner branding, but two DLLs to rebuild after every patch, and both would draw windows on the same Insert key.
3. **Rename the console to Forge.** It breaks the config file path and the Nexus page identity for the 698 existing downloads.

**Goals:**

- All 61 unused editor commands reachable from the UI.
- A command search (Ctrl+F) across every console command, existing ones included.
- An edit log showing ✓ posted / ✗ rejected, with Undo where the game has an opposite command.
- Recipes: record, save, list and replay, placed relative to the capital or at absolute tiles.
- A self-check reporting how many commands exist on the running game build.
- A release: version 2.0.0, README, Nexus BBCode, screenshots, and a game badge for 1.0.239.

**Non-goals:**

- Terrain-type or elevation painting. No editor command exists for it.
- Multiplayer support.
- Hand-made maps from scratch.
- Native game-UI widgets. IMGUI stays.

**Anti-goals (failure even if it "works"):**

- A destructive command (destroy a settlement or army, clear the tech queue) fires from the search with no confirmation.
- Typing in the search or a filter triggers game hotkeys.
- A recipe from another build or map posts invalid tile indexes instead of skipping and reporting them.
- An existing 1.7 control stops working.

**Constraints:**

- The game is Unity 6000.3.18f1 Mono on build 1.0.239.
- `TreatWarningsAsErrors` is true.
- Simulation types are internal, so they are reached through `Sim` (reflection).
- Orders are posted through `SandboxManager.PostOrder` and pre-validated by `EditorOrderProcessors.ValidateOrder` (`Orders.cs:39-63`).
- Input comes from the Unity Input System, read through `BepInEx.UnityInput.Current` (`Plugin.cs:51-54`).
- The EULA (section 10) bars commercial use, so the mod stays free.

**Left to the builder:**

- Wording of hints.
- The exact order of sections inside each tab.
- Which tabs' sections get collapsed by default.

## Global Constraints

- Plugin `Version` and csproj `<Version>` = `2.0.0`. Keep `Guid = "angelo.el2.devconsole"` and `Name = "GeloDGreat Dev Console"`.
- Window header text: `GELODGREAT DEV CONSOLE` plus a `2.0 · ENDLESS FORGE` suffix in `Theme.Value` (amber).
- Window width goes from `660f` to `780f`. Tabs are 14, in 2 rows of 7, in this order:
  - **Row 1:** Quick, Economy, Build, Heroes, Equipment, Empire, Yields
  - **Row 2:** World, Settlements, Units, AI & Story, Diplomacy, Battle, Recipes
- Search hotkey config: `General.SearchHotkey`, default `Ctrl+F`.
- Recipes folder: `BepInEx/config/forge-recipes/*.json`. Recipe format version is `1`.
- Every new order goes through `Orders.Post`. Never call `SandboxManager.PostOrder` directly.
- Destructive commands need a second click within 3 seconds ("Click again to confirm"). The search palette never runs them on Enter; it jumps to them.
- `Theme.Danger` hover text must reach at least 4.5:1 contrast on `Theme.Hover` (#2F3E50). Use `#F2A493`.
- Comments: 1–2 lines and only the non-obvious why (your COMMENT MANDATE). Run prettier on every changed `.md`/`.html`.
- Commits go on branch `feature/endless-forge`. **No push or PR without Angelo saying so.** Commits and PRs are made as `theangeloumali` only.

## Review Focus

1. **Typing in the search or a filter field leaks keys to the game** (WASD camera, hotkeys). Expected: while a console text field has focus, the game receives no keyboard shortcuts. Pinned in Task 2.4.
2. **A recipe replayed on a different or smaller map:** a relative tile falls off the map, or wraps on a non-wrapping map. Expected: the step is skipped with a "skipped: off map" line, and the remaining steps still run. Pinned in Task 1.2 (`HexGrid` tests) and Task 7.2.
3. **A recipe or undo uses an order type or field missing on this build.** Expected: on load the recipe lists the unavailable steps, replay skips them, and nothing throws. Pinned in Task 1.3 and Task 7.2.
4. **A command aimed at another empire's entity** (an AI army in the Units or AI & Story tab). Expected: the order's `EmpireIndex` is the owner's, not the local empire's. Pinned in Task 5.1 and Task 6.1.
5. **Undo after the target changed** (the dungeon was already cleared). Expected: the opposite order goes through the validator, a rejection shows ✗ in the log, and nothing crashes. Pinned in Task 7.1.

---

## How it fits together

```ascii
                 ┌──────────────────────────── DevConsole.dll (BepInEx plugin) ────────────────────────────┐
  Insert / Ctrl+F│  Plugin.cs ──► Window.cs (14 tabs, search palette, edit-log strip, self-check status)   │
                 │                    │                                                                    │
                 │   Ui/Tab*.cs ──────┼──► Commands registry (label, tab, section, run, tile?, danger?)     │
                 │      │             │            ▲ indexed by Forge.Core.CommandIndex (search ranking)   │
                 │      ▼             ▼                                                                    │
                 │   Cheats/*.cs ──► Orders.Post ──► ValidateOrder ──► SandboxManager.PostOrder ──► game  │
                 │                    │  │                                                                 │
                 │                    │  └──► OrderCodec (reflection snapshot ↔ OrderRecord)               │
                 │                    │              │                    │                               │
                 │                    ▼              ▼                    ▼                               │
                 │               EditLog (✓/✗)   InverseTable (undo)   RecipeRecorder / RecipePlayer       │
                 └───────────────────────────────────────────────────────┬────────────────────────────────┘
                                                                         │ JSON (format 1)
  Forge.Core.dll (no game refs, unit-tested): HexGrid · OrderRecord · Recipe codec · InverseTable · CommandIndex · EditLog
                                                                         │
                                                       BepInEx/config/forge-recipes/<name>.json
```

## File structure

| File                                                                                      | Status           | Responsibility                                                                                                                                                               |
| ----------------------------------------------------------------------------------------- | ---------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `plugin/Forge.Core/Forge.Core.csproj`                                                     | Create           | `net472` library with no game references. References only Newtonsoft.Json from `$(Managed)`                                                                                  |
| `plugin/Forge.Core/HexGrid.cs`                                                            | Create           | Converts between tile index and offset/cube coordinates; relative offset ↔ absolute index; map-edge and wrap rules                                                           |
| `plugin/Forge.Core/OrderRecord.cs`                                                        | Create           | A game-free order snapshot: type name, scalar fields, tile fields, empire fields                                                                                             |
| `plugin/Forge.Core/RecipeCodec.cs`                                                        | Create           | Recipe model and JSON read/write (format 1)                                                                                                                                  |
| `plugin/Forge.Core/InverseTable.cs`                                                       | Create           | Maps an order type to its opposite order type and field mapping                                                                                                              |
| `plugin/Forge.Core/CommandIndex.cs`                                                       | Create           | Search ranking over command labels and keywords                                                                                                                              |
| `plugin/Forge.Core/EditLog.cs`                                                            | Create           | Bounded log of entries (ok/rejected/failed, label, turn, record)                                                                                                             |
| `tests/Forge.Core.Tests/*`                                                                | Create (Phase 8) | xUnit tests for every Core type                                                                                                                                              |
| `plugin/DevConsole/DevConsole.csproj`                                                     | Modify           | Reference `Forge.Core` and copy it next to the plugin. Version 2.0.0                                                                                                         |
| `plugin/DevConsole/Orders.cs`                                                             | Modify           | Raise `Posted`/`Rejected` events with the order object, for the log and the recorder                                                                                         |
| `plugin/DevConsole/OrderCodec.cs`                                                         | Create           | Reflection between an `EditorOrder`/`Order` object and an `OrderRecord`                                                                                                      |
| `plugin/DevConsole/Commands.cs`                                                           | Create           | Command registry and command type                                                                                                                                            |
| `plugin/DevConsole/SelfCheck.cs`                                                          | Create           | Counts the command order types and fields present on this build                                                                                                              |
| `plugin/DevConsole/InputGuard.cs`                                                         | Create           | Blocks game keyboard shortcuts while a console text field has focus                                                                                                          |
| `plugin/DevConsole/Catalog.cs`                                                            | Modify           | Adds catalogs: Collectibles, MinorFactions, TormentedArmies, Dialogs, Constructibles                                                                                         |
| `plugin/DevConsole/Sim.cs`                                                                | Modify           | Adds `ArmyAt(int tile)`, `SettlementAt(int tile)`, `EntityGuidAt(int tile, string kind)`, `EmpireIndexOf(object entity)`, `UnitsOf(object army)`, `CapitalTile()` and `Turn` |
| `plugin/DevConsole/Cheats/Places.cs`                                                      | Create           | The 24 world-content commands                                                                                                                                                |
| `plugin/DevConsole/Cheats/Settlements.cs`                                                 | Create           | The 9 settlement commands                                                                                                                                                    |
| `plugin/DevConsole/Cheats/Units.cs`                                                       | Create           | The 7 unit and army commands                                                                                                                                                 |
| `plugin/DevConsole/Cheats/AiStory.cs`                                                     | Create           | The 21 AI, story and empire-state commands                                                                                                                                   |
| `plugin/DevConsole/Cheats/Recipes.cs`                                                     | Create           | Recorder, player and file I/O                                                                                                                                                |
| `plugin/DevConsole/Ui/PlacesSections.cs`                                                  | Create           | The new World sections, drawn from `TabWorld`                                                                                                                                |
| `plugin/DevConsole/Ui/TabSettlements.cs`, `TabUnits.cs`, `TabAiStory.cs`, `TabRecipes.cs` | Create           | The new tabs                                                                                                                                                                 |
| `plugin/DevConsole/Ui/SearchPalette.cs`, `EditLogStrip.cs`                                | Create           | Search overlay and log strip                                                                                                                                                 |
| `plugin/DevConsole/Ui/Window.cs`, `Widgets.cs`, `Theme.cs`, existing `Tab*.cs`            | Modify           | Width, tab rows, palette and log hosting; command-aware `Widgets`; danger contrast; existing buttons registered as commands                                                  |
| `plugin/DevConsole/Plugin.cs`, `State.cs`                                                 | Modify           | Version, new tabs, search hotkey, guard and self-check wiring                                                                                                                |
| `README.md`, `docs/nexus-description.txt`, `docs/images/*`                                | Modify           | Release copy and screenshots                                                                                                                                                 |

**Route: inline.** There is one owner. Every phase needs the same running game instance for its in-game check, and Phases 2–7 all touch `Window.cs`, `Catalog.cs` and `Sim.cs`, so parallel agents would contend for both.

## Status and deviations (2026-11-20)

- **60 commands, not 61.** `EditorOrderPolicyRule` is an internal order-filter class in `Amplitude.Mercury.Sandbox`, not an editor command.
- **One DLL.** `Forge.Core` sources compile into `DevConsole.dll`, so players still install a single file; `tests/Forge.Core.Tests` builds the same sources.
- **Search index:** built by drawing every tab off-screen for the one frame the palette opens, instead of registering each button by hand.
- **Grid layout and wrap** are read at runtime (the game's `TileIndexToAxialCoord` and `WorldPosition.WorldWrap`), not determined by an in-game experiment.
- **In-game checks batched.** Civilization VII was running when Phase 0 started, so the game was not launched over that session. Every phase is build-verified (warnings are errors) and `Forge.Core` has 22 passing tests; the in-game checks of Phases 0–7, the screenshots and the E2E video run together once Angelo okays launching the game.
- **Bug found by the tests:** recipe timestamps shifted by the local UTC offset (`JObject.Parse` date handling); fixed.
- **Bug found by /simplify:** AI & Story passed a hero's list position as `HeroIndex`; it now uses the hero's global index.

## Phase ledger

- [ ] Phase 0: Baseline on game 1.0.239 (compiles against 1.0.239; in-game smoke batched, see Status). **2h with Claude | 4h without** — built; in-game check pending
- [ ] Phase 1: `Forge.Core` library. **4h | 10h** — built; in-game check pending
- [ ] Phase 2: Shell v2 (command registry, 14 tabs, search, edit log, self-check, input guard). **7h | 18h** — built; in-game check pending
- [ ] Phase 3: World › Places (23 commands; Build foundation moved to Settlements). **5h | 12h** — built; in-game check pending
- [ ] Phase 4: Settlements tab (9 commands + Build foundation). **4h | 10h** — built; in-game check pending
- [ ] Phase 5: Units tab (7 commands). **5h | 12h** — built; in-game check pending
- [ ] Phase 6: AI & Story tab (20 commands; `EditorOrderPolicyRule` is an order filter, not a command). **6h | 16h** — built; in-game check pending
- [ ] Phase 7: Undo and Recipes. **6h | 16h** — built; in-game check pending
- [ ] Phase 8: Release and settle (tests, /simplify, docs, code review done; in-game checks, screenshots and E2E video wait for Angelo's go-ahead to launch the game) (tests, /simplify, review, Codex, E2E video, vault, checklist). **7h | 16h**

**Total: 46h with Claude | 114h without (60% saved).** The proposal estimated 30h / 80h for Forge before the code was read. The increase covers the input guard, registering the existing tabs' buttons as commands so search covers them, the self-check, and the Phase 0 re-verification on 1.0.239. The figures include the 50% buffer for unexplored game code from your estimation guidelines.

Every phase runs the same **Verify** command:

`set -o pipefail; python3 tools/build_plugin.py 2>&1 | tail -20`

It must exit 0, then pass the in-game check described in that phase. The in-game check:

1. Launch the game and load the save `auto-save 238`, or any mid-game save.
2. Press Insert and run the phase's commands.
3. Grep `BepInEx/LogOutput.log` for `posted <label>` or `rejected` lines.

> **Build-tool note:** `tools/build_plugin.py` resolves the Windows `dotnet.exe` and installs into `D:\SteamLibrary\...\BepInEx\plugins\`. Run it from WSL with the repo at `/mnt/c/Users/Angelo/Desktop/Work/EndlessLegent2Mods`. The first task of Phase 0 confirms that path works from WSL. If it doesn't, use `cmd.exe /c py tools\build_plugin.py`.

---

## Phase 0: Baseline on 1.0.239

### Task 0.1: Branch, build and smoke the 1.7.0 console

**Files:** none created. Modify only what breaks.

- [ ] **Step 1: Create the branch.**
  - Run `git -C /mnt/c/Users/Angelo/Desktop/Work/EndlessLegent2Mods switch -c feature/endless-forge`.
  - Confirm that `git config user.email` = `angelo@theangeloumali.com` and that `gh auth status` shows `theangeloumali`.
- [ ] **Step 2: Build.** Run the Verify command. Expected: exit 0, and `DevConsole.dll` copied to `BepInEx/plugins/`.
- [ ] **Step 3: In-game smoke.** Run one action per existing tab:
  - Quick: instant build
  - Economy: Dust Set 5000
  - Build: refresh settlements
  - Heroes: give XP
  - Equipment: add an item
  - World: spawn a unit on the target tile
  - Diplomacy: meet everybody
  - Empire: add a status
  - Battle: one toggle
  - Yields: Dust x2

  Expected: 10 `posted …` lines in `LogOutput.log` and no `not found in this game build` warnings.

- [ ] **Step 4: Fix any break the smoke shows** (a renamed member found by `Sim.Method`, a missing order type), then re-run Step 3.
- [ ] **Step 5: Commit** (only if Step 4 changed code): `fix: restore <control> on game 1.0.239`.

## Phase 1: `Forge.Core` library

### Task 1.1: Project scaffold

**Files:**

- Create: `plugin/Forge.Core/Forge.Core.csproj`. `net472`, `LangVersion latest`, `TreatWarningsAsErrors true`. Reference `Newtonsoft.Json` with HintPath `$(Managed)\Newtonsoft.Json.dll` and `Private=false`. Reuse the same `GameDir` and `Managed` properties as `DevConsole.csproj`.
- Modify: `plugin/DevConsole/DevConsole.csproj`. Add `<ProjectReference Include="..\Forge.Core\Forge.Core.csproj" />`.
- Modify: `tools/build_plugin.py`. Copy `Forge.Core.dll` alongside `DevConsole.dll`.

- [ ] Step 1: Create the csproj and the reference. Run the Verify command. Expected: exit 0, and both DLLs in `BepInEx/plugins/`.
- [ ] Step 2: Commit: `build: add Forge.Core library`.

### Task 1.2: `HexGrid`

**Interfaces, produces:**

- `public readonly struct HexGrid`, constructed as `HexGrid(int width, int height, HexLayout layout, bool wrapsX)`.
- `public enum HexLayout { OddRow, OddColumn }`
- `public readonly struct HexOffset { public readonly int Dq, Dr; }`, in cube axial deltas.
- `HexOffset Relative(int fromTile, int toTile)`
- `bool TryApply(int anchorTile, HexOffset offset, out int tile)`. Returns false when the result is off the map. A non-wrapping map never wraps.
- Tile index = `row * width + column`, as the game uses (`World.TileCount` = width × height, `World.cs:191-204`).

- [ ] Step 1: Implement. Cube conversion follows the standard offset ↔ axial formulas for both layouts. `TryApply` wraps the column modulo `width` only when `wrapsX` is set.
- [ ] Step 2: Verify command exit 0. (Prototype: unit tests land in Phase 8, Task 8.1. The test names below are fixed now.)
  - `Relative_ThenApply_RoundTrips_OnBothLayouts`
  - `TryApply_OffTopEdge_ReturnsFalse`
  - `TryApply_PastRightEdge_WrapsOnlyWhenWrapsX`
  - `TryApply_OnSmallerMap_FallsOffAndReturnsFalse` (Review Focus #2)
- [ ] Step 3: Commit: `feat(core): hex grid relative placement`.

### Task 1.3: `OrderRecord` and `RecipeCodec`

**Interfaces, produces:**

- `public sealed class OrderRecord`. Fields:
  - `string Type` (simple type name, e.g. `EditorOrderSpawnObservatoryAt`)
  - `Dictionary<string, JToken> Scalars`
  - `Dictionary<string, int> Tiles`. Absolute indexes, for every int field whose name ends in `TileIndex`.
  - `HashSet<string> LocalEmpireFields`. `EmpireIndex`-style fields equal to the local empire at record time.
- `public sealed class Recipe`. Fields:
  - `int Format = 1`
  - `string Name`, `string GameBuild`, `DateTime CreatedUtc`
  - `RecipeAnchor Anchor` (`Capital` | `Absolute`)
  - `int AnchorTile`
  - `List<RecipeStep> Steps`
- `public sealed class RecipeStep`. Fields:
  - `OrderRecord Order`
  - `Dictionary<string, HexOffset> RelativeTiles`
- `public static class RecipeCodec`:
  - `string Write(Recipe recipe)`
  - `Recipe Read(string json, out List<string> problems)`. An unknown `Format` reports a problem instead of throwing.

- [ ] Step 1: Implement with Newtonsoft (`JObject`). The JSON property names equal the C# member names.
- [ ] Step 2: Verify command exit 0. Phase 8 tests:
  - `Write_ThenRead_RoundTripsEveryField`
  - `Read_UnknownFormat_ReportsProblem`
  - `Read_MissingStepType_ReportsProblemAndKeepsOtherSteps` (Review Focus #3)
- [ ] Step 3: Commit: `feat(core): order records and recipe format 1`.

### Task 1.4: `InverseTable`, `CommandIndex`, `EditLog`

**Interfaces, produces:**

`InverseTable`:

- `public static bool TryInvert(OrderRecord done, Func<string, int> currentValue, out OrderRecord undo)`.
- Static pairs, with the same tile fields copied across:

  | Done                                      | Undo                                                                     |
  | ----------------------------------------- | ------------------------------------------------------------------------ |
  | `EditorOrderCreateArmyAt`                 | `EditorOrderDestroyArmy` (`ArmyTileIndex` ← `TileIndex`)                 |
  | `EditorOrderCreateCityAt`                 | `EditorOrderDestroySettlement` (`SettlementTileIndex` ← `CityTileIndex`) |
  | `EditorOrderCreateCampAt`                 | `EditorOrderDestroySettlement` (← `CampTileIndex`)                       |
  | `EditorOrderCreateVillageAt`              | `EditorOrderDestroyVillageAt`                                            |
  | `EditorOrderCreateDistrictAt`             | `EditorOrderRemoveDistrictAt`                                            |
  | `EditorOrderSpawnObservatoryAt`           | `EditorOrderRemoveObservatoryAt`                                         |
  | `EditorOrderSpawnBurrowAt`                | `EditorOrderRemoveBurrowAt`                                              |
  | `EditorOrderSpawnDungeonOrSpawnPointAt`   | `EditorOrderRemoveDungeonOrSpawnPointAt`                                 |
  | `EditorOrderCreateVillageSpawnLocationAt` | `EditorOrderRemoveVillageSpawnLocationAt`                                |
  | `EditorOrderCreateCuriosityAt`            | `EditorOrderRemoveCollectibleAt`                                         |
  | `EditorOrderPlantForest`                  | `EditorOrderCutForest` (`DestinationTileIndex`)                          |
  | `EditorOrderPathCuriosityAdd`             | `EditorOrderPathCuriosityRemove`                                         |

- Stock setters `EditorOrderSetMoneyStock` and `EditorOrderSetInfluenceStock` invert to themselves with the value captured before posting, through `currentValue`.
- Everything else has no undo.

`CommandIndex`:

- `public sealed class CommandIndex<T>`
- `void Add(T item, string label, string location, IEnumerable<string> keywords)`
- `IReadOnlyList<T> Search(string query, int max = 8)`
- Ranking: prefix-of-word match, then substring of the label, then keyword match. Ties go alphabetically.

`EditLog`:

- `public sealed class EditLog`, constructed as `EditLog(int capacity = 200)`
- `void Add(EditEntry entry)`
- `IReadOnlyList<EditEntry> Latest(int n)`
- `int Count`
- `public sealed class EditEntry { int Turn; string Label; EditOutcome Outcome; OrderRecord Record; OrderRecord Undo; }`
- `enum EditOutcome { Posted, Rejected, Failed, Skipped }`

- [ ] Step 1: Implement all three.
- [ ] Step 2: Verify command exit 0. Phase 8 tests:
  - `TryInvert_SpawnObservatory_GivesRemoveAtSameTile`
  - `TryInvert_ClearMountain_ReturnsFalse`
  - `TryInvert_SetMoney_UsesCapturedPreviousValue`
  - `Search_WordPrefixOutranksSubstring`
  - `EditLog_OverCapacity_DropsOldest`
- [ ] Step 3: Commit: `feat(core): undo table, command search, edit log`.

## Phase 2: Shell v2

### Task 2.1: Command registry and command-aware widgets

**Interfaces, produces:**

- `internal sealed class Command { string Tab; string Section; string Label; string[] Keywords; Action Run; Action<int> RunAtTile; bool Destructive; string OrderType; }`. Exactly one of `Run` and `RunAtTile` is set.
- `internal static class Commands`:
  - `Command Add(...)`
  - `IReadOnlyList<Command> All`
  - `CommandIndex<Command> Index`
- Overloads `Widgets.Button(Command)`, `Widgets.Plot(Command)` and `Widgets.Row(params Command[])`.
  - A `Destructive` command shows "Click again to confirm" for 3 s after the first click.
  - Each section's `Rect` y-position is recorded in `EventType.Repaint` (`GUILayoutUtility.GetLastRect`) so the search can scroll to it.

**Steps:**

- [ ] Step 1: Add `Commands.cs` and the overloads in `Widgets.cs`. The existing overloads stay for non-command controls such as Refresh.
- [ ] Step 2: Register every existing actionable button in the 10 existing tabs as a `Command`, in each tab's constructor. Each tab's `Draw` then uses the command overloads.
  - Labels stay identical to 1.7.
  - Mark destructive: `Clear every queue`, `Dismiss`, `Clear equipment`, `Force surrender`.
- [ ] Step 3: Verify command exit 0. In game, every existing tab still works: repeat the Task 0.1 Step 3 smoke and expect 10 `posted` lines.
- [ ] Step 4: Commit: `refactor: register console buttons as searchable commands`.

### Task 2.2: Window v2 (14 tabs, header, theme contrast)

**Files:** Modify `Ui/Window.cs`, `Ui/Theme.cs`, `Plugin.cs`, `State.cs`, `DevConsole.csproj`.

- [ ] Step 1: Window changes.
  - `Width = 780f`.
  - `Tabs()` draws 2 rows of 7 in the Global Constraints order.
  - Header adds the `2.0 · ENDLESS FORGE` label in `Theme.Value`.
- [ ] Step 2: Theme. Add `Danger.hover.textColor = #F2A493`.
- [ ] Step 3: Plugin and State.
  - `Version = "2.0.0"`.
  - Add placeholder tab classes `TabSettlements`, `TabUnits`, `TabAiStory` and `TabRecipes`. Each draws a one-line hint until its phase fills it.
  - `State.SearchHotkey = config.Bind("General", "SearchHotkey", new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl), …)`.
- [ ] Step 4: Verify command exit 0. In game, 14 tabs show in 2 rows, and the header matches the mock-up.
- [ ] Step 5: Commit: `feat: console 2.0 shell with 14 tabs`.

### Task 2.3: Edit log strip and the `Orders` events

**Interfaces:**

- `Orders.Posted` is `event Action<object, string>` (order, label). `Orders.Rejected` is `event Action<object, string, string>` (order, label, reason).
- `OrderCodec.Snapshot(object order) → OrderRecord`.
- `OrderCodec.Build(OrderRecord record) → object`. Returns null when the type or a field is missing on this build.
- `Sim.Turn`, read reflectively from the sandbox.

**Steps:**

- [ ] Step 1: Raise the events.
  - `Posted` fires in `Orders.Send` after `post()` succeeds.
  - `Rejected` fires in `Allowed` when it returns false, and from `Send`'s catch with outcome `Failed`.
- [ ] Step 2: Implement `OrderCodec`.
  - Use reflection over the order's public instance fields.
  - Encode `StaticString` as a string and enums by name.
  - Ints ending in `TileIndex` go to `Tiles`.
  - Ints named `EmpireIndex`, `MajorEmpireIndex` or `OwnerEmpireIndex` that equal `Sim.LocalEmpireIndex` go to `LocalEmpireFields`.
  - Arrays and `SimulationEntityGUID` fields set `Scalars["$unrecordable"] = true`.
- [ ] Step 3: `Ui/EditLogStrip.cs` draws the last 5 entries under the scroll view, as in the mock-up:
  - turn
  - ✓ or ✗
  - label, plus the reason when rejected
  - an Undo button (disabled until Phase 7)
- [ ] Step 4: Verify command exit 0. In game:
  - Spawn a unit: one ✓ row appears.
  - Spawn a village on water: one ✗ row appears with "rejected".
- [ ] Step 5: Commit: `feat: edit log of posted and rejected orders`.

### Task 2.4: Search palette and input guard

**Interfaces:**

- `Ui/SearchPalette.cs`:
  - `void Toggle()`
  - `bool Open`
  - `void Draw(Rect windowRect)`
  - Queries `Commands.Index`.
  - **Enter** runs a non-destructive `Run` command, or arms a `RunAtTile` command via `Cheats.World.Arm`.
  - **Tab** jumps: switch tab, scroll to the section y, close.
  - On a destructive command, Enter only jumps.
- `InputGuard`:
  - `bool TextFocused` is set when `GUIUtility.keyboardControl != 0` during the console's `OnGUI`.
  - While it is set, game keyboard shortcuts are blocked.

**Steps:**

- [ ] Step 1: Find out whether typing in the existing Dropdown "Filter" field moves the camera or fires hotkeys.
  - Type `wasd` into the Equipment filter and watch the map.
  - Record the result in the commit message.
- [ ] Step 2: If keys leak, find the game's shortcut entry point.
  - Use ILSpy on `Amplitude.Mercury.Firstpass.dll` and search for `InputAction`/`Shortcut`/`KeyboardShortcut` handling in the presentation layer.
  - Add a Harmony prefix that returns false while `InputGuard.TextFocused`.
  - If no single entry point exists, disable the game's `InputActionAsset` maps while focused and re-enable them on blur.
- [ ] Step 3: Implement the palette. The search hotkey from `State.SearchHotkey` toggles it in `Plugin.Update`, and it opens the window if hidden. Esc closes it.
- [ ] Step 4: Verify command exit 0. In game:
  - Ctrl+F, type `dung`: dungeon commands are listed.
  - Typing does not move the camera (Review Focus #1).
  - Enter arms "Spawn dungeon".
  - Tab on "Destroy settlement" jumps without running it.
- [ ] Step 5: Commit: `feat: command search with game input guard`.

### Task 2.5: Self-check

**Interfaces:** `SelfCheck.Run() → (int found, int total, List<string> missing)`. It runs once on the first `Orders.Ready`. For each distinct `Command.OrderType`, it checks `AccessTools.TypeByName("Amplitude.Mercury.Interop." + type)` and the fields the cheat uses.

- [ ] Step 1: Implement it. Show the result in the window's status line: `Self-check: N / M commands found on <Application.version>`, green when complete and amber otherwise. Missing names are logged once.
- [ ] Step 2: Verify command exit 0. In game the status line reads `M / M`.
- [ ] Step 3: Commit: `feat: self-check of commands against the running build`.

## Phase 3: World › Places (24 commands)

### Task 3.1: Catalogs and `Cheats/Places.cs`

**Interfaces, produces:**

New catalogs:

- `Catalog.Collectibles` → `Entries<CollectibleDefinition, CollectibleDefinitionUIMapper>(None)`. If the mapper type name differs, use the `UIMapper` base.
- `Catalog.MinorFactions` → `MinorFactionDefinition`.
- `Catalog.TormentedArmies` → `TormentedArmyTypeDefinition`.

`Places` (each posts through `Orders.Post`, labelled `"<verb> at tile {t}"`):

- **Curiosities and collectibles**
  - `CreateCuriosity(string name, int t)`
  - `RemoveCollectible(int t)`
- **Dungeons**
  - `SpawnDungeon(int t, int danger)`
  - `RemoveDungeon(int t)`
- **Observatories**
  - `SpawnObservatory(int t)`
  - `EvolveObservatory(int t, string evolveTo)`
  - `RemoveObservatory(int t)`
- **Burrows and coral**
  - `SpawnBurrow(int t)`
  - `RemoveBurrow(int t)`
  - `CreateCoralSpore(int t)`
- **Tormented**
  - `CreateTormented(int t, string armyType, bool isTormented, bool onlyTormented)`
  - `FillTormentedGauge(int t)`
  - `ToggleTormentedSettle(int t, bool settled)`. Resolves `TormentedGUID` from the settlement or army at the tile through `Sim`.
- **Village spawn points**
  - `AddVillageSpawnPoint(int t, string minorFaction)`
  - `RemoveVillageSpawnPoint(int t)`
- **Terrain and districts**
  - `RefreshRiver(int t)`
  - `RemoveDistrict(int t)`
  - `BuildFoundation(int t)`. Resolves `SettlementGUID` from the settlement that owns the tile.
- **Roads**
  - `SetRoadQuality(int quality)`
  - `ForceRoadUpdate()`
- **Path curiosities**
  - `PathCuriosityAdd(int pathIndex, int t, string definition)`
  - `PathCuriosityAddPath()`
  - `PathCuriosityRemove(int t)`
  - `PathCuriosityRemovePath(int pathIndex)`

`OwnerEmpireIndex` and `EmpireIndex` fields use `Sim.LocalEmpireIndex`. Field names follow `unused-editor-order-fields-1.0.239.txt` exactly.

**Steps:**

- [ ] Step 1: Field types the file doesn't show (`EvolveTo`, `RoadQuality`, `DangerLevel`, `CuriosityDefinition`): read them with ILSpy on `Amplitude.Mercury.Firstpass.dll`.
  - For an enum, expose an enum dropdown.
  - For a `StaticString` with no matching database, expose a text field and add a hint naming an example value.
- [ ] Step 2: Implement `Places`, and `Sim.SettlementAt(int tile)` / `Sim.EntityGuidAt(int tile, string kind)`.
- [ ] Step 3: Commit: `feat: world content commands`.

### Task 3.2: `Ui/PlacesSections.cs`

- [ ] Step 1: Draw the "Places" and "Terrain extras" sections as in the mock-up. Call it from `TabWorld.Draw()` after the existing Map section.
  - Register every control as a `Command` (`Tab = "World"`).
  - Remove buttons are not marked destructive: they are undoable or cheap.
  - Path curiosities go in a collapsed "Advanced" section.
- [ ] Step 2: Verify command exit 0. In game, place and then remove each of these on valid tiles. Every place/remove pair shows ✓✓ in the log:
  - a curiosity
  - a dungeon at danger 3
  - an observatory
  - a burrow
  - a Tormented army
  - a village spawn point
- [ ] Step 3: Commit: `feat: Places sections in the World tab`.

## Phase 4: Settlements tab (9 commands)

### Task 4.1: `Cheats/Settlements.cs` and `Ui/TabSettlements.cs`

**Interfaces:**

- `Settlements.Destroy(int settlementTile)` (Destructive)
- `SetCapital(int cityTile)`
- `Rename(int settlementTile, string name)`
- `MergeInto(int cityTile, int mergedTile)`
- `DetachTerritory(int cityTile, int adminCenterTile)`. Note the game's misspelled field `AdiministrativeCenterToDetachTileIndex`.
- `EvolveCampToCity(int campTile)`
- `Enqueue(int settlementTile, string constructible, int constructionTile, int position)`
- `RemoveConstruction(int settlementTile, int constructionIndex)`
- `ChangePopulation(int settlementTile, string populationName)`

`Catalog.Constructibles` is `Districts ∪ Units ∪ Improvements`, ordered by `Rank`.

**Steps:**

- [ ] Step 1: Implement. The tab works like this:
  - "Pick settlement on the map" (Plot) sets the selected settlement tile.
  - The selection shows the name, kind, tile and whether it is the capital (`Sim.SettlementAt`).
  - Controls follow the mock-up.
  - The production queue table reads the queue reflectively. If the queue isn't readable on this build, show only the "Remove by index" field.
- [ ] Step 2: Verify command exit 0. In game:
  - Rename a city, then make another city the capital.
  - Enqueue a district, then remove it.
  - Destroy a test camp: the first click shows the confirm text, the second click posts.
- [ ] Step 3: Commit: `feat: Settlements tab`.

## Phase 5: Units tab (7 commands)

### Task 5.1: `Cheats/Units.cs` and `Ui/TabUnits.cs`

**Interfaces:**

- `Units.Add(int armyTile, string unitDefinition)`
- `Remove(int armyTile, int unitIndex)`
- `Evolve(SimulationEntityGUID unit, string evolution)`
- `Damage(SimulationEntityGUID army, SimulationEntityGUID[] units, int amount)`
- `DestroyArmy(int armyTile)` (Destructive)
- `SetMovementRatio(int armyTile, float ratio)`
- `EnterBurrow(SimulationEntityGUID army, int destinationTile, SimulationEntityGUID exitBurrow)`

Supporting reads:

- `Sim.ArmyAt(int tile)` finds any empire's army.
- `Sim.EmpireIndexOf(object army)` gives the owner (Review Focus #4).
- `Sim.UnitsOf(object army)` returns `(name, guid, hp, maxHp)`.

**Steps:**

- [ ] Step 1: Use ILSpy to find where a unit's available evolutions live (`UnitEvolutionInfo` / `RequestUnitEvolutionTree` in Firstpass). Fill the "Evolve ▸" list from it. If that isn't reachable, use a text field plus a hint.
- [ ] Step 2: Implement. "Pick army on the map" selects the army on the tile, whoever owns it, and the unit table follows the mock-up.
- [ ] Step 3: Verify command exit 0. In game:
  - Add a unit to your own army and then remove it.
  - Damage an AI army by 50.
  - Set the movement ratio to 2.0.
  - Destroy a test army after confirming.
  - Check the log: the `EmpireIndex` on the AI-army orders equals the AI's index, not 0.
- [ ] Step 4: Commit: `feat: Units tab`.

## Phase 6: AI & Story tab (21 commands)

### Task 6.1: `Cheats/AiStory.cs` and `Ui/TabAiStory.cs`

**Interfaces** (`EmpireIndex` arguments come from an empire picker reused from `TabDiplomacy`):

- **Armies**
  - `SetArmyMission(int armyTile, ArmyMissionTypes type, int targetTile)`
  - `AttackCity(int armyTile, int cityTile, int attackerEmpire)`
- **Relations**
  - `ForceAlliance(int a, int b)`
  - `MoodMessage(int source, int target, string mood)`
  - `ClearMoodDeadzone(int source, int target, bool clearExclude)`
- **Story**
  - `StartDialog(string dialog, int empire)`
- **Dungeons** (each resolves the dungeon GUID from the tile)
  - `ClearDungeon(int t)`
  - `RegenerateDungeon(int t, int seed)`
  - `ContinueCrawl(int t)`
- **Heroes**
  - `DeployHero(int heroIndex, int empire, int t)`
  - `DismissHero(int heroIndex, int empire)`
  - `RedistributeHeroes()`
  - `PartnerDen(int empire, SimulationEntityGUID hero, int t)`
- **Minor factions**
  - `ReplaceMinorTrait(int minorIndex, int traitIndex, bool cohabitation)`
- **Empire state**
  - `SetEmpireState(string definition, int empire)`
  - `ReadyEmpireAction(EmpireActionType type, int empire)`
  - `UnlockSeat(SimulationEntityGUID seat, int empire)`
  - `BypassSgeLevel(int empire, int level)`
- **Technology**
  - `EnqueueTechnology(int empire, string tech, int position)`
  - `ClearTechQueue(int empire)` (Destructive)
- **Policy**
  - `PolicyRule(…)`. Its fields come from ILSpy in Step 1. If it carries no settable fields, register it as a no-argument command.

Catalogs: `Catalog.Dialogs` → `DialogDefinition`. Mood names come from `DiplomaticMoodMessageUIMappers`. Empire states use the database found in Step 1.

**Steps:**

- [ ] Step 1: Use ILSpy to confirm the field types for `ArmyMissionTypes`, `ForceAlliance`, `PolicyRule`, `SetEmpireStateDefinition`, `UnlockSeat` and `BypassSGELevel`, and the definition database behind each name argument. Any name argument without a database gets a text field plus a hint.
- [ ] Step 2: Implement. The sections are as in the mock-up. The hero index picker lists the selected empire's heroes through `Sim.Collection(empire, "Heroes")`.
- [ ] Step 3: Verify command exit 0. In game:
  - Give an AI army an Attack mission on your city: it moves toward it at end of turn.
  - Force an alliance between two AIs: it shows in Diplomacy.
  - Start a faction dialog: it opens.
  - Clear a dungeon.
  - Enqueue a technology, then clear the tech queue after confirming.
- [ ] Step 4: Commit: `feat: AI & Story tab`.

## Phase 7: Undo and Recipes

### Task 7.1: Undo

- [ ] Step 1: Capture the "before" value.
  - On `Orders.Posted`, snapshot the record and call `InverseTable.TryInvert`.
  - `currentValue` reads the current Dust and Influence stock through `Sim`, **before** posting. Add a `Orders.Posting` event raised just before `post()`.
- [ ] Step 2: The `EditLogStrip` Undo button posts `OrderCodec.Build(entry.Undo)` through `Orders.Post`.
  - If `Build` returns null, show "undo unavailable on this build".
  - A rejection shows as a new ✗ row (Review Focus #5).
- [ ] Step 3: Verify command exit 0. In game:
  - Spawn an observatory, then Undo: it is removed, ✓.
  - Set Dust to 1,000,000, then Undo: Dust returns to the previous value.
  - Clear a dungeon, then Undo its earlier spawn: ✗ "rejected", with no exception in `LogOutput.log`.
- [ ] Step 4: Commit: `feat: undo for invertible edits`.

### Task 7.2: Recipes

**Interfaces:**

- `Recipes.StartRecording()`, `Stop()`, `bool Recording`.
- `Recipes.Save(string name) → string path`.
- `Recipes.List() → IReadOnlyList<(string name, int steps, string build)>`.
- `Recipes.Replay(string name, RecipeAnchor anchor) → (int posted, int skipped, List<string> reasons)`.
- The anchor is `Sim.CapitalTile()`.
- The `HexGrid` comes from `WorldMapProvider` width and height (as `World.TileCount` reads them). Layout and wrap are confirmed in Step 1.

**Steps:**

- [ ] Step 1: Confirm the grid layout and horizontal wrap.
  - Spawn a unit at tile `t`, then at `t + 1` and `t + width`, and compare their on-map positions.
  - Check whether a unit can move across the left/right edge.
  - Record the result as constants in `Cheats/Recipes.cs`, with a one-line comment giving the evidence.
- [ ] Step 2: Implement.
  - While recording, every `Orders.Posted` record without `$unrecordable` becomes a step, with `RelativeTiles` computed from the capital.
  - Replay (each step is posted through `Orders.Post` so it is validated):
    - rebuild each step
    - map tiles through `HexGrid.TryApply`, skipping off-map steps with the reason "off map"
    - substitute `LocalEmpireFields` with the current local index
    - skip a step with the reason "not on this build" when `OrderCodec.Build` returns null
  - Show a summary in the log: `Recipe fortress-start: 12 posted, 2 skipped`.
- [ ] Step 3: `Ui/TabRecipes.cs`, as in the mock-up: Record/Stop, name field, Save, the saved list with Replay buttons, "Replay at the same tiles", and "Open folder" (`Application.OpenURL(file://…)`).
- [ ] Step 4: Verify command exit 0. In game:
  - Record 5 edits (spawn an army, place a dungeon, an observatory, a village spawn point, set Dust) and save `smoke-test`.
  - Start a new game on a **smaller** map and replay it relative to the capital. All in-bounds steps show ✓, and any off-map step shows "skipped: off map" (Review Focus #2).
  - Hand-edit one step's `Type` to `EditorOrderDoesNotExist`. Replay shows "skipped: not on this build" and the other steps still post (Review Focus #3).
- [ ] Step 5: Commit: `feat: recipes record and replay`.

## Phase 8: Release and settle (the prototype debts are paid here)

### Task 8.1: `Forge.Core` tests

**Files:** Create `tests/Forge.Core.Tests/Forge.Core.Tests.csproj` (`net472`, xUnit, references `Forge.Core`), plus test files per Core type.

- [ ] Step 1: Write every test named in Tasks 1.2–1.4. Assertions use the exact values from those tasks.
- [ ] Step 2: Run `set -o pipefail; dotnet.exe test tests/Forge.Core.Tests 2>&1 | tail -15`. Expected: all pass.
- [ ] Step 3: Commit: `test: Forge.Core unit tests`.

### Task 8.2: Release copy

- [ ] Step 1: README.
  - Badge `game-V1.0.239`.
  - New sections: Settlements, Units, AI & Story, Places, Search, Edit log & Undo, Recipes.
  - Install steps unchanged, plus "copy both `DevConsole.dll` and `Forge.Core.dll`".
- [ ] Step 2: `docs/nexus-description.txt`. Update the BBCode with the same sections and the version 2.0.0.
- [ ] Step 3: New screenshots in `docs/images/`: `settlements.png`, `units.png`, `ai-story.png`, `recipes.png`, `search.png`, plus `world.png` reshot. Add them to the README table.
- [ ] Step 4: Run prettier on the changed `.md`. Commit: `docs: 2.0 Endless Forge release notes`.

### Task 8.3: Completion mandate (all deferred checks)

- [ ] `/simplify` on the branch diff, then re-run the Verify command.
- [ ] Code review: `Skill(code-review)` on the branch, plus `bash ~/.claude/hooks/codex-review.sh`. Fix findings at the root and re-run.
- [ ] Reuse audit and `bash ~/.claude/hooks/dead-code-check.sh`.
- [ ] Feature checklist `EL2-FORGE-review-checklist.md` in `~/Desktop/eor-feature-checklists/`: every phase's in-game check marked Pass or Blocked.
- [ ] E2E video, last. Follow `Skill(zkidzdevteam:demo-recording)`.
  - Format: MP4 H.264 at the native resolution, saved to `recordings/`.
  - The run: one continuous take through the Places, Settlements, Units and AI & Story tabs, search, undo and a recipe replay. Frame-verified.
  - It goes into `recordings/` (gitignored for `.webm`; the MP4 stays untracked) and is never referenced in a commit or PR.
- [ ] `bash ~/.claude/hooks/vault-append.sh` for the learnings.
- [ ] Stop. Report to Angelo. **Push, PR and Nexus upload wait for his go-ahead.** When he authorizes a PR, the body carries `Verification deferred (prototype): tests, code review, E2E video run before merge`, ticked as each one passes.

## Deploy

Nothing is pushed or published by this plan.

- When Angelo authorizes it, push `feature/endless-forge` to `origin` (the GitHub and NAS push URLs) and open a PR to `main`.
- Release packaging (`dist/DevConsole.zip` containing both DLLs) and the Nexus upload are a separate step that Angelo triggers.

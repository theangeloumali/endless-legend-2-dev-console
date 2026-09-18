# Dev Console — ENDLESS Legend 2

An in-game developer console for feature testing. Two variants share this repo:

| Variant                                  | What it is                                                                                                       | Use it when                                                      |
| ---------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| **Plugin** (`plugin/`)                   | BepInEx + Harmony plugin: press **Insert**, one window with every cheat, buttons apply on click, toggles persist | you want the WeMod-style console                                 |
| **Data mod** (`mod/`, `spec/`, `tools/`) | Official JSON modding path only (`ModdingGuide_EL2_1.1.pdf`): every-turn event dialogs with up to 4 buttons      | you need something shareable on the Workshop / no code injection |

Both target build **V1.0.116**. Run one or the other — the data mod's every-turn dialog is redundant once the plugin is in.

## Plugin

```
python tools/build_plugin.py            # dotnet build (net472) → BepInEx/plugins/DevConsole.dll
python tools/install_mod.py --uninstall # remove the data mod so its every-turn dialog stops appearing
```

Requires BepInEx 5.4 already installed in the game folder and a .NET SDK (`%USERPROFILE%\.dotnet` or on PATH).
Installing parks every other plugin DLL into `BepInEx/plugins.disabled/` — the Nexus Resource Manager patches the
same `Gain*` methods and would double the multipliers.

Press **Insert** (configurable) to open or hide the window. Eight tabs, everything applies on press:

| Tab       | What it does                                                                                                                                                                                         |
| --------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Economy   | Set Dust / Influence / city cap to a typed value · add research · every strategic, luxury and special resource · unlock an era or a single named technology                                          |
| Build     | **Instant build and instant recruit** — complete any queued item free and immediately, per settlement or across the empire                                                                           |
| Heroes    | Roster dropdown with an **apply-to-all** toggle · give skill points · raise stats · give XP · heal · draw N heroes at a chosen level range · recruit them · spawn any named hero at the hovered tile |
| Equipment | Stash listing · add any of the 177 equipment definitions (filterable, or in bulk) · equip to a hero, unequip per slot · clear everything                                                             |
| World     | Targets **the tile under your mouse** — spawn any unit, found a city or camp, teleport an army, set god speed, reveal the whole map, collect every curiosity                                         |
| Diplomacy | Force war / peace / treaties / surrender · war score · meet everybody · pacify minor empires · pick a victory path · **switch which empire you play**                                                |
| Battle    | Amplitude's own seven cheats — infinite movement, infinite action tokens, infinite battle skills, ignore zone of control / round count / empire playing, line-of-sight debug                         |
| Yields    | Per-turn multipliers (×2…×1000) for Dust, Industry, Science, Influence, plus instant build/research                                                                                                  |

### How it works

**Writes go through the game's own order system.** Amplitude shipped their in-house editor in the retail build and
left it `public`: ~110 `EditorOrder*` types plus the god/cheat orders in `Amplitude.Mercury.Interop`, dispatched by
`SandboxManager.PostOrder`, which enqueues onto a thread-safe queue that the simulation drains and validates. So the
console is not injecting values behind the game's back — it asks the simulation the same way the developers' editor
did. `Orders.cs` is the whole dispatch layer.

**Reads still use reflection**, because `Sandbox`, `Empire`, `Hero`, `Settlement` and `Army` are all `internal`.
`Sim.cs` is that bridge, and every member is resolved by name and null-checked, so a rename on a future patch
disables one control instead of breaking the plugin.

Two things are _not_ orders. Yield multipliers stay Harmony patches on the `Gain*` methods because no order scales
per-turn income — and since orders are processed asynchronously, a console "+research" is multiplied too if the
Science multiplier is on. Battle cheats call `BattleDebug.SetCheat` directly with `writeRegistry: false`, so they
never outlive the session.

Only your empire is affected: everything targets `Sandbox.LocalEmpireIndex`, which the game keeps current on
hot-seat swaps. Dropdown contents come from the live datatables, so a game patch or a data mod is picked up
automatically. Settings persist in `BepInEx/config/angelo.el2.devconsole.cfg`; `UiScale` defaults to auto (×2 at 4K).

### Known gap — the native debug overlay

The game also ships Amplitude's own debug-overlay UI, gated by `DebugOverlayManager.IsEnabled`, which is compiled to
`return false`. The plugin patches that to `true` (config: `NativeOverlay`), and the patch applies — but **F2 does not
bring the overlay up on V1.0.116**: `DebugOverlayManager` is a framework `Manager` whose start-up path registers the
key binding, and it does not appear to run in the retail build. The IMGUI window above is unaffected. Set
`NativeOverlay = false` to skip the patch entirely.

Layout: `Plugin.cs` entry · `Ui/` window shell, shared widgets and one file per tab · `Cheats/` one file per area ·
`Orders.cs` dispatch · `Catalog.cs` datatable names · `Sim.cs` reflection reads · `Patches.cs` yield multipliers ·
`DebugOverlay.cs` the overlay unlock.

## Data mod

Six console pages appear **every turn** for human empires (AI never sees them). Each page is an event dialog
with up to 4 buttons; one action per page per turn. Pick a button → **Confirm Path** → close the summary.

| Page      | Buttons                                                                                                                  |
| --------- | ------------------------------------------------------------------------------------------------------------------------ |
| Economy   | +10,000 Dust · +100,000 Dust · +10,000 Influence · +100,000 Influence                                                    |
| Research  | +10,000 Research · +100,000 Research · Unlock Era I–III techs · Unlock Era IV–VII techs                                  |
| Resources | +10,000 all Strategic · +10,000 all Luxury · +10,000 Cadavers & Spirits · +1,000 of everything (stock caps lifted first) |
| Yields    | Yields ×10 · ×100 · ×1000 · ×1 (off) — Dust, Industry, Science, Influence, Food in every city                            |
| Instant   | Instant build ON/OFF (Industry ×1000) · Instant research ON/OFF (Science ×1000)                                          |
| Combat    | Invulnerable (10 turns) · One-hit kills (10 turns) · Both · +10,000 Dust                                                 |

### Install

```
python tools/build_devconsole.py <export-dir>      # regenerate mod/DevConsole from spec/devconsole.py
python tools/validate_mod.py  <export-dir>         # static checks against the game export
python tools/install_mod.py --source mod/DevConsole [--park-bepinex]
python tools/install_mod.py --uninstall            # remove it again
```

`<export-dir>` is the extracted `Public\ModdableAssets\assets_data.zip` (31k JSON files). The installer copies the
mod to `%localappdata%\Amplitude Studios\Endless Legend 2\Modding\DevConsole` and writes `mod-configuration.json`.
Start a **new game** after installing, updating or removing: saves keep references to mod elements, and a renamed or
removed element crashes the loaded game. `--park-bepinex` moves any BepInEx plugin out of the way so it cannot
confound numbers; `--restore-bepinex` puts it back.

### Adding a button

Edit `spec/devconsole.py` (pages → buttons → `(kind, *args)` effects), rebuild, validate, install. Kinds:
`money`, `influence`, `research`, `unlock_era`, `resources`, `lift_caps`, `yields`, `yields_off`, `apply_status`,
`remove_status`, `apply_status_on_units`. Max 4 buttons per page; every RawValue must stay below 2^31.

### Known limits (all inherited from the data-only path)

- The Select → **Confirm Path** two-step is the game's event UI; data cannot make a button apply on first click.
- One action per page per turn. Pages that re-open in the same turn were tried three ways (consequence events
  auto-confirm their first button; bus-triggered events never fired mid-turn); see `spike/`.
- Multipliers stop at ×1000: the simulation uses Int32 fixed-point (×1000), so a stock or yield above 2,147,483
  overflows. Dust/Influence stock caps at ≈2.1 M for the same reason. The plugin has the same ceiling.
- Console statuses are hidden (no icon): the game refuses to start when a visible `StatusDefinition` has no
  `StatusUIMapper` it can find, and mod-added mappers are not found by that check.
- Cadavers/Spirits only exist for their factions; the buttons are harmless elsewhere.

## Layout

```
plugin/DevConsole/         BepInEx plugin (C#, net472) — Ui/ tabs, Cheats/ one file per area
tools/build_plugin.py      dotnet build + install into BepInEx/plugins (parks other plugins)
spec/devconsole.py         the data-mod console, declaratively
tools/odin.py              Odin SerializationNodes tree model (parse / serialize / re-id) — round-trips the whole export
tools/el2mod/              exemplar loader, effect builders, asset builders
tools/build_devconsole.py  spec -> mod/DevConsole
tools/validate_mod.py      static checks
tools/install_mod.py       install / uninstall / verify / park BepInEx
mod/DevConsole/            generated data mod (committed: inspectable and uploadable as-is)
spike/                     the hand-shaped experiments that established the mechanics (reference only)
docs/plans/                plan of record
```

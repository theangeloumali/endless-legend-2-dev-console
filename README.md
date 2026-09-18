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
python tools/install_mod.py --uninstall # remove the data mod so its dialog stops appearing
```

Requires BepInEx 5.4 already installed in the game folder and a .NET SDK (`%USERPROFILE%\.dotnet` or on PATH).
Installing parks every other plugin DLL into `BepInEx/plugins.disabled/` — the Nexus Resource Manager patches the
same `Gain*` methods and would double the multipliers.

In game press **Insert** (configurable) to open or hide the window:

| Section           | Controls                                                                    | How it works                                                                                                                      |
| ----------------- | --------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| Economy           | +10,000 / +100,000 Dust · Influence · Research                              | `DepartmentOfTheTreasury.GainMoney`, `DepartmentOfCulture.GainInfluence`, `DepartmentOfScience.GainResearch`                      |
| Resources         | amount field · All strategic · All luxury · Cadavers & Spirits · Everything | `DepartmentOfResources.GiveGodAccessToResource` (ignores stock caps)                                                              |
| Technologies      | Era I–VII · All                                                             | `DepartmentOfScience.UnlockAllTechnologies(era)`                                                                                  |
| Yield multipliers | off / ×2 / ×10 / ×100 / ×1000 for Dust, Industry, Science, Influence        | Harmony prefix on the `Gain*` methods, postfix on `DepartmentOfIndustry.ComputeProductionIncome`; applied at the next income tick |
| Instant           | Instant build · Instant research                                            | Industry / Science ×1000                                                                                                          |
| Combat            | Invulnerable · One-hit kills · Heal all armies                              | Harmony prefix on `BattleUnit.ApplyDamage`; `IDamageableEntity.SetHealthRatio(1)`                                                 |

Only your empire is affected: the plugin reads the game's own `Sandbox.LocalEmpireIndex`, which the game keeps
current on hot-seat swaps, so AI empires never see a cheat. Toggles and the amount persist in
`BepInEx/config/angelo.el2.devconsole.cfg`. Every game member is resolved by name and null-checked, so a renamed method
on a future patch disables that one control (warning in `BepInEx/LogOutput.log`) instead of breaking the plugin. Each
press logs `<Department>: applied to empire #N`. The plugin declares itself incompatible with the Nexus Resource
Manager (`com.yourname.el2resourcemanager`); BepInEx refuses to load both.

Layout: `Plugin.cs` entry + hotkey · `Window.cs` IMGUI · `Actions.cs` instant actions · `Patches.cs` Harmony patches ·
`State.cs` config · `Sim.cs` reflection bridge to the internal simulation types.

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
plugin/DevConsole/         BepInEx plugin (C#, net472)
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
